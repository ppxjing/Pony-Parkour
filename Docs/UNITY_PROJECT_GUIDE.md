# Unity 工程使用说明

工程位置：D:/6412_Group/2026s1-project-2-team-02
Unity 版本：6000.3.19f1（Unity 6.3 LTS）

## 目录约定
- Assets/_Project/：自行开发的内容，分为 Scenes、Scripts、Prefabs、Materials、Textures、Audio、Animations、Settings、Editor。
- Assets/ThirdParty/FunObstacleCourseVol1/：机关包，示例为 Scenes/ExampleScene.unity。
- Assets/ThirdParty/Minicar_Env_Pack/：关卡环境包，Demo 下有厨房、卧室、浴室、海滩演示场景；Assets/Scenes 下有 StarterKit 场景。
- 6412_Assets/：原始 unitypackage 安装包，本地保留，Git 忽略。
- 3DRunner-main/：历史参考内容，保留原样，不属于本工程 Assets 导入目录。
- Docs/：项目说明。

当前沿用原工程的 Built-in 渲染管线。先在 Unity 中浏览各包示例，再在 _Project/Scenes 建立自己的关卡。修改资源请先复制到 _Project，尽量保留第三方包原样。资源移动和重命名请在 Unity 的 Project 窗口执行，保证 .meta/GUID 配对。

## Git 协作
本次整理分支：setup/unity63-assets。原有提交历史和 origin 远程保留。
Assets、Packages、ProjectSettings 与所有 .meta 需要提交。
Library、Temp、Logs、UserSettings、构建结果和原始安装包不提交。
模型、贴图、音频采用 Git LFS；场景、预制体与 .meta 保留文本差异。
新电脑先安装 Git LFS，再运行 git lfs install、git clone、git lfs pull，然后使用 Unity 6000.3.19f1 打开工程根目录。
本次只做本地提交，不推送远程。发布第三方资源前请核对资源许可与仓库访问权限。

## 开始开发
打开 Assets/_Project/Scenes/StartScene.unity。它是原工程的起始场景，尚未制作第三人称角色或组合关卡。
两个资源包已共同放入同一 Assets 目录；导入素材不等于已经制作完整游戏。

## 材质适配说明
机关包原始 HDRP 材质已适配为 Built-in Standard，保留主贴图、颜色及法线，映射金属度和光滑度标量。HDRP 专用效果和打包遮罩未做逐像素等效转换；原始 unitypackage 可用于恢复。示例场景的 HDRP 专用组件可能仍需在实际关卡制作时替换。本次不代表所有示例场景已完成视觉验收。
