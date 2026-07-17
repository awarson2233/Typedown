# Copilot Instructions

## 项目指南
- 在 Typedown 仓库做性能优化时，先运行性能探查器定位热点；优化目标应限定为 `Typedown.WinUI`，不要优化 `Dev/Typedown`。
- 在 Typedown 仓库中，Typedown.Editor 前端产物采用手动构建，不应由 Visual Studio/MSBuild 在构建 Typedown.WinUI 或解决方案时自动编译。