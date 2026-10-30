using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace UIBuilderUssNavigator
{
    /// <summary>
    /// 示例的导入与打开入口。UPM 安装沿用 Unity 原生 Sample API，首次复制到 Assets/Samples，
    /// 再次打开复用已导入内容，不覆盖用户的练习修改。兼容 unitypackage 使用导出时附带的 Samples。
    /// 页面本体完全由 UXML/USS 定义；打开交给原生资产处理器，保留 UI Builder 的未保存修改询问。
    /// </summary>
    internal static class ExamplesLauncher
    {
        internal const string SampleName = "Learning Lab";

        /// <param name="page">包内受控的相对页面名，例如 01-Navigation/Navigation，不包含语言后缀。</param>
        internal static bool Open(string page)
        {
            string root;
            PackageInfo package = PackageInfo.FindForAssembly(typeof(ExamplesLauncher).Assembly);
            if (package != null)
            {
                Sample sample = Sample.FindByPackage(package.name, package.version)
                    .Single(candidate => candidate.displayName == SampleName);
                if (!sample.isImported && !ImportSample(sample)) return false;
                // Sample 返回本机分隔符，FileUtil 的资源路径接口要求正斜杠。
                root = FileUtil.GetProjectRelativePath(sample.importPath.Replace('\\', '/'));
            }
            else
            {
                // Assets 安装允许用户移动插件目录；从本脚本资产定位根目录，而非绑定机器绝对路径。
                string script = AssetDatabase.FindAssets("ExamplesLauncher t:MonoScript")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Single(path => Path.GetFileName(path) == "ExamplesLauncher.cs");
                root = Path.GetDirectoryName(Path.GetDirectoryName(script)) + "/Samples/LearningLab";
            }

            string assetPath = (root + "/" + page + "." + L10n.Language + ".uxml").Replace('\\', '/');
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            if (asset == null)
                throw new FileNotFoundException(L10n.Tr(
                    "示例文件已被移动或删除。请从 Package Manager 重新导入 Learning Lab，或恢复该文件。",
                    "The example file was moved or deleted. Reimport Learning Lab from Package Manager or restore the file."), assetPath);
            bool firstOpen = ExampleCanvasSettings.IsFirstOpen(asset);
            bool opened = AssetDatabase.OpenAsset(asset);
            if (opened && firstOpen) ExampleCanvasSettings.InitializeAfterOpen(asset);
            return opened;
        }

        static bool ImportSample(Sample sample)
        {
            string versionRoot = Path.GetDirectoryName(sample.importPath);
            string samplesRoot = Path.GetDirectoryName(versionRoot);
            bool hasEarlierImport = Directory.Exists(samplesRoot) && Directory.EnumerateDirectories(samplesRoot)
                .Any(version => Directory.Exists(Path.Combine(version, SampleName)));
            if (!hasEarlierImport) return sample.Import(Sample.ImportOptions.None);

            // Unity 6000.3 的 Import(None) 遇到旧版本会直接失败；OverridePreviousImports 会删除旧练习。
            // 本包只有普通 UXML/USS 资产，因此只复制到尚不存在的当前版本目录，保留旧版本及其修改。
            Directory.CreateDirectory(versionRoot);
            FileUtil.CopyFileOrDirectory(sample.resolvedPath, sample.importPath);
            AssetDatabase.Refresh();
            return true;
        }
    }
}
