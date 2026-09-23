import { describe, expect, it } from 'vitest';
import { EditorState } from '@codemirror/state';
import { FreezeTimer, isComposeTransaction, hasRefresh, refreshReveal, setRevealFrozen, revealFrozen } from '../src/editor/decorations/revealState';

describe('鼠标冻结计时', () => {
  function harness() {
    const events: boolean[] = [];
    const timers: { f: () => void; ms: number; live: boolean }[] = [];
    const t = new FreezeTimer(v => events.push(v), 100,
      (f, ms) => { const h = { f, ms, live: true }; timers.push(h); return h; },
      h => { (h as { live: boolean }).live = false; });
    const fire = () => timers.filter(h => h.live).forEach(h => { h.live = false; h.f(); });
    return { t, events, timers, fire };
  }
  it('按下立即冻结，松开后 100 ms 解冻', () => {
    const { t, events, timers, fire } = harness();
    t.down();
    expect(events).toEqual([true]);
    t.up();
    expect(timers.at(-1)!.ms).toBe(100);
    expect(events).toEqual([true]);
    fire();
    expect(events).toEqual([true, false]);
  });
  it('解冻前再次按下：取消计时，保持冻结，不重复通知', () => {
    const { t, events, fire } = harness();
    t.down(); t.up(); t.down();
    fire();
    expect(events).toEqual([true]);
    t.up(); fire();
    expect(events).toEqual([true, false]);
  });
  it('没有按下时松开不产生事件', () => {
    const { t, events, fire } = harness();
    t.up(); fire();
    expect(events).toEqual([]);
  });
});

describe('组字与刷新的事务判定', () => {
  const s = EditorState.create({ doc: 'ab', extensions: [revealFrozen] });
  it('input.type.compose 是组字事务，普通输入不是', () => {
    expect(isComposeTransaction(s.update({ changes: { from: 1, insert: '中' }, userEvent: 'input.type.compose' }))).toBe(true);
    expect(isComposeTransaction(s.update({ changes: { from: 1, insert: 'x' }, userEvent: 'input.type' }))).toBe(false);
  });
  it('刷新效果与解冻都要求重算显形', () => {
    expect(hasRefresh(s.update({ effects: refreshReveal.of(null) }))).toBe(true);
    expect(hasRefresh(s.update({ effects: setRevealFrozen.of(false) }))).toBe(true);
    expect(hasRefresh(s.update({ effects: setRevealFrozen.of(true) }))).toBe(false);
    expect(s.update({ effects: setRevealFrozen.of(true) }).state.field(revealFrozen)).toBe(true);
  });
});
