using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Editor.Wire;

namespace Typedown.CoreTests.Editor;

/// <summary>
/// 本地图片请求的路径校验：页面已消解相对路径，宿主只接受原样合规的绝对路径与图片扩展名，其余一律拒绝。
/// </summary>
[TestClass]
public sealed class LocalImageRequestTests
{
    [TestMethod]
    public void Accepts_EncodedAbsolutePath()
    {
        var r = LocalImageRequest.Parse("GET", "https://typedown.image/c/Users/me/%E5%9B%BE%201.png?v=1#x");
        Assert.AreEqual(LocalImageRequestKind.Accepted, r.Kind);
        Assert.AreEqual(@"C:\Users\me\图 1.png", r.Path);
        Assert.AreEqual("image/png", r.ContentType);

        Assert.AreEqual("image/svg+xml", LocalImageRequest.Parse("HEAD", "https://TYPEDOWN.IMAGE/D/a.SVG").ContentType);
        Assert.AreEqual(@"D:\a%b.jpg", LocalImageRequest.Parse("GET", "https://typedown.image/D/a%25b.jpg").Path);
    }

    [TestMethod]
    public void Ignores_OtherHosts()
    {
        Assert.AreEqual(LocalImageRequestKind.NotLocalImage, LocalImageRequest.Parse("GET", "https://typedown.editor/index.html").Kind);
        Assert.AreEqual(LocalImageRequestKind.NotLocalImage, LocalImageRequest.Parse("GET", "https://typedown.image.evil.com/C/a.png").Kind);
        Assert.AreEqual(LocalImageRequestKind.NotLocalImage, LocalImageRequest.Parse("GET", "http://typedown.image/C/a.png").Kind);
    }

    [TestMethod]
    public void Rejects_NonGetMethods()
    {
        Assert.AreEqual(LocalImageRequestKind.MethodNotAllowed, LocalImageRequest.Parse("POST", "https://typedown.image/C/a.png").Kind);
    }

    [TestMethod]
    [DataRow("https://typedown.image/C/a/../b.png")]            // 相对段
    [DataRow("https://typedown.image/C/a/%2E%2E/b.png")]        // 编码的相对段
    [DataRow("https://typedown.image/C/./b.png")]
    [DataRow("https://typedown.image/C/a//b.png")]              // 空段
    [DataRow("https://typedown.image/C/a%2F..%2Fb.png")]        // 段内编码的分隔符
    [DataRow("https://typedown.image/C/a%5C..%5Cb.png")]
    [DataRow("https://typedown.image/C/a.png%3Azone")]          // 备用数据流
    [DataRow("https://typedown.image/C/a%00.png")]              // 控制字符
    [DataRow("https://typedown.image/C/dir./a.png")]            // 以点结尾的段
    [DataRow("https://typedown.image/C/dir%20/a.png")]          // 以空格结尾的段
    [DataRow("https://typedown.image/C/NUL.png")]               // 设备名
    [DataRow("https://typedown.image/C/dir/com1.txt.png")]
    [DataRow("https://typedown.image/C/secret.txt")]            // 不是图片
    [DataRow("https://typedown.image/C/a.png.exe")]
    [DataRow("https://typedown.image/C/")]                      // 只有盘符
    [DataRow("https://typedown.image/C")]
    [DataRow("https://typedown.image/server/share/a.png")]      // UNC 形态
    [DataRow("https://typedown.image/1/a.png")]
    [DataRow("https://typedown.image/CC/a.png")]
    public void Rejects_UnsafePaths(string uri)
    {
        Assert.AreEqual(LocalImageRequestKind.Forbidden, LocalImageRequest.Parse("GET", uri).Kind, uri);
    }
}
