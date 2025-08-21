using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class PrivySDKPostprocessBuild
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
#if UNITY_IOS
        if (buildTarget == BuildTarget.iOS)
        {
            var projPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projPath));

            // Link the AuthenticationServices framework, used by OAuth flows on iOS.
            var frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(frameworkTargetGuid, "AuthenticationServices.framework", false);
            File.WriteAllText(projPath, project.WriteToString());

            // Add the 'Sign in with Apple' capability to the Player target, necessary for displaying the native flow.
            var mainTargetGuid = project.GetUnityMainTargetGuid();
            var capabilitiesManager = new ProjectCapabilityManager(projPath, "Unity-iPhone/Unity-iPhone.entitlements",
                targetGuid: mainTargetGuid);
            capabilitiesManager.AddSignInWithApple();
            capabilitiesManager.WriteToFile();
        }
#endif
    }
}