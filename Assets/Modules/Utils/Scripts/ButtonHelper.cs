using MixedReality.Toolkit.UX;
using UnityEngine;

namespace IMLD.MixedRealityAnalysis.Utils
{
    public static class ButtonHelper
    {
        public static void SetText(GameObject button, string text)
        {
            var child = button.transform.FindRecursively("IconAndText");
            if (child == null) return;

            var textComponent = child.GetChild(0).GetComponent<TMPro.TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }

        public static Transform FindRecursively(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var result = FindRecursively(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}