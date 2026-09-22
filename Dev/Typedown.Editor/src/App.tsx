import React from 'react';
import Editor from 'components/Editor';
import ErrorBoundary from 'components/ErrorBoundary';
import 'services/theme'
import 'services/scrollbar'
import 'services/localization'
import 'services/keyboard'
import './App.scss';

// 右键菜单交给宿主：WinUI3 下 WebView2 自己吃掉了指针事件，XAML 看不到右键，
// 只能靠 CoreWebView2.ContextMenuRequested 转成原生 Flyout。
// 页面一旦 preventDefault 掉 contextmenu，那个事件就不会触发，所以这里不能再拦。
// UWP 版是手工把指针消息转发给 WebView 的，XAML 天然收得到右键，才有条件在页面侧屏蔽。

function App() {
  return (
    <ErrorBoundary>
      <Editor />
    </ErrorBoundary>
  );
}

export default App;
