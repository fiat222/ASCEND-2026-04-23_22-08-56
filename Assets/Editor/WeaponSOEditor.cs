using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponSO))]
public class WeaponSOEditor : Editor
{
    static readonly string[] _excluded = { "projectilePrefab", "projectileSpeed", "projectileRange", "projectileForce", "manaCost" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, _excluded);

        WeaponSO so = (WeaponSO)target;
        bool isProjectile = so.weaponType is WeaponType.Bow or WeaponType.Staff or WeaponType.Wand or WeaponType.Spear;

        if (isProjectile)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Projectile", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectilePrefab"));

            bool physicsLaunch = so.weaponType is WeaponType.Bow or WeaponType.Spear;
            bool magicLaunch   = so.weaponType is WeaponType.Staff or WeaponType.Wand;

            if (physicsLaunch)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileForce"));

            if (magicLaunch)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("manaCost"));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
