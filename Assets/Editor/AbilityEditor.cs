using UnityEditor;
using UnityEngine;

// This attribute tells Unity which class this editor customizes
[CustomEditor(typeof(Ability))]
public class AbilityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Cast the target to your class to access its members
        Ability ability = (Ability)target;
        
        // Calculate totalLifetime and display it
        EditorGUI.BeginDisabledGroup(true); // Makes the next field read-only
        EditorGUILayout.FloatField("Projectile Lifetime", ability.range / ability.speed);
        EditorGUILayout.FloatField("Total Lifetime", (ability.range / ability.speed) + ability.maxLingeringLifetime);
        EditorGUI.EndDisabledGroup();
    }
}
