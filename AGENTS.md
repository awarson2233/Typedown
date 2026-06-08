# Typedown 项目本地协作指令

## 构建验证

- 进行 .NET / WinUI 构建验证时，必须使用本机环境中的 **ARM64 MSBuild**。
- 不要使用 x64/x86 MSBuild 代替 ARM64 构建验证。
- 如果当前 shell 无法解析 ARM64 MSBuild 路径，应先定位本机 Visual Studio / Build Tools 的 ARM64 MSBuild，再执行验证。
- subagent 在实现或 review 结束时，如果需要构建验证，应明确记录是否已使用 ARM64 MSBuild；若未执行，应说明原因。
