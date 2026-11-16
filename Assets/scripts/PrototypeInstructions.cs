using UnityEngine;

namespace NeonVeil
{
    // Temporary milestone UI. Replace with Canvas menus during the interface milestone.
    public class PrototypeInstructions : MonoBehaviour
    {
        private void OnGUI()
        {
            GUI.Box(new Rect(15, 15, 470, 100), "NEON VEIL — Movement Prototype");
            GUI.Label(new Rect(30, 42, 440, 65),
                "A/D or arrows: move     Space: jump (hold for height)\n" +
                "Cross the platforms to the cyan extraction marker.\n" +
                "Falling returns you to the start. Combat comes next.");
        }
    }
}
