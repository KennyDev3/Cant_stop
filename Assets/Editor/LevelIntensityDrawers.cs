using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EnemyDirector.LevelIntensityProfile), true)]
public class LevelIntensityProfileDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty profileName = property.FindPropertyRelative("profileName");
        string name = profileName != null && !string.IsNullOrEmpty(profileName.stringValue)
            ? profileName.stringValue
            : label.text;

        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            name,
            true
        );

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.BeginProperty(position, label, property);
            if (profileName != null)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), profileName, new GUIContent("Profile Name (e.g. World_1)"));
                y += EditorGUIUtility.singleLineHeight + 2;
            }
            SerializedProperty bands = property.FindPropertyRelative("bands");
            if (bands != null)
            {
                float bandsHeight = EditorGUI.GetPropertyHeight(bands, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, bandsHeight), bands, true);
            }
            EditorGUI.EndProperty();
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

        float h = EditorGUIUtility.singleLineHeight + 2;
        if (property.FindPropertyRelative("profileName") != null) h += EditorGUIUtility.singleLineHeight + 2;
        SerializedProperty bands = property.FindPropertyRelative("bands");
        if (bands != null) h += EditorGUI.GetPropertyHeight(bands, true);
        return h;
    }
}

[CustomPropertyDrawer(typeof(EnemyDirector.LevelIntensityBand), true)]
public class LevelIntensityBandDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty atMinute = property.FindPropertyRelative("atMinute");
        SerializedProperty maxEnemies = property.FindPropertyRelative("maxEnemies");
        float minVal = atMinute != null ? atMinute.floatValue : 0;
        int maxVal = maxEnemies != null ? maxEnemies.intValue : 0;
        string bandLabel = $"Enemies allowed at minute {minVal:F1}: {maxVal}";

        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            bandLabel,
            true
        );

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            Rect contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, position.height);
            EditorGUI.BeginProperty(contentRect, label, property);
            float y = contentRect.y;
            if (atMinute != null)
            {
                EditorGUI.PropertyField(new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight), atMinute, new GUIContent("At Minute"));
                y += EditorGUIUtility.singleLineHeight + 2;
            }
            if (maxEnemies != null)
            {
                EditorGUI.PropertyField(new Rect(contentRect.x, y, contentRect.width, EditorGUIUtility.singleLineHeight), maxEnemies, new GUIContent("Max Enemies"));
            }
            EditorGUI.EndProperty();
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;

        float h = EditorGUIUtility.singleLineHeight + 2;
        SerializedProperty atMinute = property.FindPropertyRelative("atMinute");
        if (atMinute != null) h += EditorGUIUtility.singleLineHeight + 2;
        SerializedProperty maxEnemies = property.FindPropertyRelative("maxEnemies");
        if (maxEnemies != null) h += EditorGUIUtility.singleLineHeight + 2;
        return h;
    }
}
