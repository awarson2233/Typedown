import { describe, expect, it } from 'vitest';
import { Keymap, installShortcutBridge, readModifiers, shouldBypass, type KeyLike } from '../src/host/keyboard';

const key = (over: Partial<KeyLike>): KeyLike => ({
  key: 's', keyCode: 83, ctrlKey: false, altKey: false, shiftKey: false, metaKey: false, isComposing: false, defaultPrevented: false, ...over,
});

describe('快捷键放行规则（与旧 keyboard.ts 一致）', () => {
  it('输入法组字（isComposing 或 keyCode 229）一律放行', () => {
    expect(shouldBypass(key({ isComposing: true, ctrlKey: true }))).toBe(true);
    expect(shouldBypass(key({ keyCode: 229, key: 'Process', ctrlKey: true }))).toBe(true);
  });

  it('已被外层处理（defaultPrevented）的放行', () => {
    expect(shouldBypass(key({ defaultPrevented: true, ctrlKey: true }))).toBe(true);
  });

  it('单独按下的修饰键放行', () => {
    for (const k of ['Shift', 'Control', 'Alt', 'Meta']) expect(shouldBypass(key({ key: k, keyCode: 17, ctrlKey: true }))).toBe(true);
  });

  it('长按重复不过滤', () => {
    expect(shouldBypass(key({ repeat: true, ctrlKey: true }))).toBe(false);
  });

  it('修饰键位与 KeyboardModifiers 一致：Control 1、Menu 2、Shift 4、Windows 8', () => {
    expect(readModifiers(key({}))).toBe(0);
    expect(readModifiers(key({ ctrlKey: true }))).toBe(1);
    expect(readModifiers(key({ altKey: true }))).toBe(2);
    expect(readModifiers(key({ shiftKey: true }))).toBe(4);
    expect(readModifiers(key({ metaKey: true }))).toBe(8);
    expect(readModifiers(key({ ctrlKey: true, shiftKey: true }))).toBe(5);
  });
});

describe('和弦表比对', () => {
  const map = new Keymap();
  map.set([{ key: 83, modifiers: 1 }, { key: 90, modifiers: 1 }, { key: 112, modifiers: 0 }, { key: 90, modifiers: 5 }]);

  it('键码与修饰键组合都相同才命中，命中时原样回报整数', () => {
    expect(map.match(key({ ctrlKey: true }))).toEqual({ key: 83, modifiers: 1 });
    expect(map.match(key({ ctrlKey: true, shiftKey: true }))).toBeNull();
    expect(map.match(key({}))).toBeNull();
    expect(map.match(key({ key: 'F1', keyCode: 112 }))).toEqual({ key: 112, modifiers: 0 });
    expect(map.match(key({ key: 'Z', keyCode: 90, ctrlKey: true, shiftKey: true }))).toEqual({ key: 90, modifiers: 5 });
  });

  it('命中的和弦在组字中、已处理时仍放行；长按重复照样拦', () => {
    expect(map.match(key({ ctrlKey: true, isComposing: true }))).toBeNull();
    expect(map.match(key({ ctrlKey: true, defaultPrevented: true }))).toBeNull();
    expect(map.match(key({ ctrlKey: true, repeat: true }))).toEqual({ key: 83, modifiers: 1 });
  });

  it('退化的注册项（单按 Ctrl）不会吞掉修饰键', () => {
    const m = new Keymap();
    m.set([{ key: 17, modifiers: 1 }]);
    expect(m.match(key({ key: 'Control', keyCode: 17, ctrlKey: true }))).toBeNull();
  });

  it('view.keymap 整表替换；空表或畸形项不拦任何键', () => {
    const m = new Keymap();
    m.set([{ key: 83, modifiers: 1 }]);
    m.set(null);
    expect(m.size).toBe(0);
    m.set([{ key: '83', modifiers: 1 } as never, { key: 83, modifiers: 1 }]);
    expect(m.size).toBe(1);
  });

  it('捕获阶段监听：命中时 preventDefault、stopPropagation 并回报', () => {
    const listeners: ((e: KeyboardEvent) => void)[] = [];
    const target = { addEventListener: (_: string, l: (e: KeyboardEvent) => void, capture: boolean) => { expect(capture).toBe(true); listeners.push(l); }, removeEventListener() {} };
    const got: unknown[] = [];
    installShortcutBridge(target as never, map, c => got.push(c));
    const ev = (over: Partial<KeyLike>) => {
      const e = { ...key(over), prevented: 0, stopped: 0, preventDefault() { this.prevented++; }, stopPropagation() { this.stopped++; } };
      listeners[0](e as unknown as KeyboardEvent);
      return e;
    };
    const hit = ev({ ctrlKey: true });
    expect([hit.prevented, hit.stopped]).toEqual([1, 1]);
    const miss = ev({ key: 'a', keyCode: 65 });
    expect([miss.prevented, miss.stopped]).toEqual([0, 0]);
    expect(got).toEqual([{ key: 83, modifiers: 1 }]);
  });
});
