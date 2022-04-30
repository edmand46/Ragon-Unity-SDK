using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sources.Editor
{
  public class ProjectBuilder : UnityEditor.Editor
  {
    public static Dictionary<BuildTarget, (string, string, string)> Executables = new()
    {
      {BuildTarget.StandaloneWindows64, ("Arena", "Builds/Clients", ".exe")},
      {BuildTarget.StandaloneOSX, ("Arena", "Builds/Clients", "")},
    };

    public static void BuildClientMacOnly() => Build(BuildTarget.StandaloneOSX, null);

    public static void BuildClientWinOnly() => Build(BuildTarget.StandaloneWindows64, null);

    public static void Build(BuildTarget target, string currentMap = null) =>
      Build(target, BuildTargetGroup.Standalone, BuildOptions.None, currentMap);

    public static void Build(BuildTarget target,
      BuildTargetGroup targetGroup = BuildTargetGroup.Standalone,
      BuildOptions options = BuildOptions.None,
      string currentMap = null)
    {
      var tuple = Executables[target];
      var executable = tuple.Item1;
      var path = tuple.Item2;
      var ext = tuple.Item3;
      var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

      EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);

      // if (target == BuildTarget.StandaloneLinux64)
      //   PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "SERVER");
      // else
      //   PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone,
      //     "CLIENT;STATS_ENABLED;UNITY_POST_PROCESSING_STACK_V2");

      if (!string.IsNullOrEmpty(currentMap)) scenes = new[] {currentMap};

      var buildOptions = new BuildPlayerOptions();
      buildOptions.scenes = scenes;
      buildOptions.target = target;
      buildOptions.options = options;
      buildOptions.locationPathName = $"{path}/{executable}{ext}";
      BuildPipeline.BuildPlayer(buildOptions);

      Debug.Log($"Build for {target} completed!");
    }
  }
}