using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Ragon.Client.Unity
{
  [InitializeOnLoad]
  public sealed class RagonDefines : UnityEditor.Editor
  {
    static readonly string[] defines = new string[] { "RAGON_NETWORK" };

    static RagonDefines()
    {
      var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
      var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTarget);
      var definesString = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
      var allDefines = definesString.Split(';').ToList();

      allDefines.AddRange(defines.Except(allDefines));

      var definesStr = string.Join(";", allDefines.ToArray());
      PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, definesStr);
    }
  }
}