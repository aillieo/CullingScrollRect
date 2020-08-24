## Culling ScrollRect

使用`CullingScrollRect`，可避免一次性实例化大量子物体，同时降低运行时内存压力。

具体使用方法参考`TestView.cs`。

运行`SampleScene`可看到如下内容，为演示剔除效果，禁用了Mask。

---

`CullingScrollRect` is designed to avoid instantiating large amount of objects at once, and reduce runtime memory usage, see `TestView.cs` for more details.

Run `SampleScene` and the following image will be presented, and the Mask is diabled to make it easier to see culling happens.

---

![img](Screenshots/testview.gif)
