using UnityEngine;
using System.IO;

namespace Hitsub.ExpressionFaceIconGenerator.Scripts.Editor
{
    public static class ExpressionFaceIconUtility
    {
        /// <summary>
        /// ディレクトリが存在するか
        /// </summary>
        /// <param name="projectPath">Assetsから始まるパス</param>
        /// <returns></returns>
        public static bool IsExistDirectory(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return false;
            }
            return Directory.Exists(GetExportFullDirectoryPath($@"{projectPath}"));
        }

        /// <summary>
        /// 絶対パスの取得
        /// </summary>
        /// <param name="projectPath">Assetsから始まるパス</param>
        /// <returns></returns>
        public static string GetExportFullDirectoryPath(string projectPath)
        {
            const string ASSET = "Assets";
            var assetPath = Application.dataPath.Remove(Application.dataPath.Length - ASSET.Length, ASSET.Length);
            var directory = $"{assetPath}{projectPath}";
            return directory;
        }
    }
}