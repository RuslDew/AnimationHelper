using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

public class AnimationEditorHelper
{
    private static System.Type AnimationWindowType;

    private static IEnumerable CachedSelectedKeyframes = null;

    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;


    private static System.Type GetAnimationWindowType()
    {
        if (AnimationWindowType == null)
        {
            AnimationWindowType = System.Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
        }

        return AnimationWindowType;
    }

    private static object GetOpenAnimationWindow()
    {
        object[] openWindows = Resources.FindObjectsOfTypeAll(GetAnimationWindowType());

        if (openWindows.Length > 0)
        {
            return openWindows[0];
        }

        return null;
    }

    [MenuItem("AnimationHelper/Copy selected keyframes &c")]
    private static void CopyKeyframes()
    {
        object animState = GetAnimWindowStateObject();
        object root = GetRootGameObject();
        CachedSelectedKeyframes = GetSelectedKeyframes();

        bool haveSelectedKeyframes = CachedSelectedKeyframes != null && ((IList)CachedSelectedKeyframes).Count > 0;

        if (animState != null && root != null && haveSelectedKeyframes)
        {
            CopyKeyframesToClipboard();
        }
    }

    [MenuItem("AnimationHelper/Paste copied keyframes &v")]
    private static void PasteKeyframes()
    {
        object animState = GetAnimWindowStateObject();
        object root = GetRootGameObject();

        if (animState != null && root != null)
        {
            MatchCurves();

            PasteKeyframesFromClipboard();
        }
    }

    private static void PasteKeyframesFromClipboard()
    {
        object animState = GetAnimWindowStateObject();

        InvokeMethod(animState, "PasteKeys", null);
    }

    private static void CopyKeyframesToClipboard()
    {
        object animState = GetAnimWindowStateObject();

        InvokeMethod(animState, "CopyKeys", null);
    }

    private static void MatchCurves()
    {
        if (CachedSelectedKeyframes != null)
        {
            GameObject root = GetRootGameObject();

            IEnumerable copiedCurves = GetCopiedCurves();

            if (copiedCurves != null)
            {
                foreach (var curve in copiedCurves)
                {
                    string path = (string)GetPropertyObject(curve, "path");
                    Type type = (Type)GetPropertyObject(curve, "type");

                    GameObject matchingGameObjectByPath = null;

                    if (path == "")
                    {
                        matchingGameObjectByPath = root;
                    }
                    else
                    {
                        matchingGameObjectByPath = root.transform.Find(path).gameObject;
                    }

                    if (GameObjectHasComponentOfType(matchingGameObjectByPath, type))
                    {
                        AddCurveToCurrentStateCurve(curve);
                    }
                }
            }

            SaveCurrentStateCurves();
        }
    }

    private static IEnumerable GetCopiedCurves()
    {
        if (CachedSelectedKeyframes != null)
        {
            List<object> copiedCurves = new List<object>();

            foreach (var keyframe in CachedSelectedKeyframes)
            {
                object keyframeCurve = GetPropertyObject(keyframe, "curve");

                if (!copiedCurves.Contains(keyframeCurve))
                {
                    object curveCopyWithoutKeyframes = GetCopyOfCurveWithoutKeyframes(keyframeCurve);

                    if (curveCopyWithoutKeyframes != null)
                    {
                        copiedCurves.Add(curveCopyWithoutKeyframes);
                    }
                }
            }

            return copiedCurves as IEnumerable;
        }

        return null;
    }

    private static object GetCopyOfCurveWithoutKeyframes(object curve)
    {
        if (curve != null)
        {
            object animationClip = GetCurrentAnimationClip();
            object binding = GetFieldObject(curve, "m_Binding");
            object valueType = GetFieldObject(curve, "m_ValueType");

            ConstructorInfo curveConstructor = curve.GetType().GetConstructor(new Type[] { curve.GetType() });

            try
            {
                object newCurve = curveConstructor.Invoke(new object[] { animationClip, binding, valueType });
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to invoke \"AnimationWindowCurve\" constructor: " + e.ToString());

                return null;
            }
        }

        return null;
    }

    private static bool CurrentStateContainsCurve(object curve)
    {
        object currentStateCurves = GetAllAnimStateCurves();

        if (currentStateCurves != null)
        {
            foreach (var item in currentStateCurves as IEnumerable)
            {
                string currentStateCurvePath = (string)GetPropertyObject(item, "path");
                Type currentStateCurveType = (Type)GetPropertyObject(item, "type");

                string curvePath = (string)GetPropertyObject(curve, "path");
                Type curveType = (Type)GetPropertyObject(curve, "type");

                if (curvePath == currentStateCurvePath && curveType == currentStateCurveType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void AddCurveToCurrentStateCurve(object curve)
    {
        if (!CurrentStateContainsCurve(curve))
        {
            InvokeMethod(GetAllAnimStateCurves(), "Add", new object[] { curve });
        }
    }

    private static void SaveCurrentStateCurves()
    {
        object animState = GetAnimWindowStateObject();

        InvokeMethod(animState, "SaveCurves", new object[] { GetCurrentAnimationClip(), GetAllAnimStateCurves(), null });
    }

    private static object GetCurrentAnimationClip()
    {
        object animState = GetAnimWindowStateObject();
        object selection = GetFieldObject(animState, "m_Selection");
        object animationClip = GetPropertyObject(selection, "animationClip");

        return animationClip;
    }

    private static bool GameObjectHasComponentOfType(GameObject gameObject, Type type)
    {
        if (gameObject != null)
        {
            Component component = gameObject.GetComponent(type);

            if (component != null)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable GetAllAnimStateCurves()
    {
        object animWindowStateObject = GetAnimWindowStateObject();

        return GetFieldObject(animWindowStateObject, "m_AllCurvesCache") as IEnumerable;
    }

    private static IEnumerable GetSelectedKeyframes()
    {
        object animWindowStateObject = GetAnimWindowStateObject();

        return GetFieldObject(animWindowStateObject, "m_SelectedKeysCache") as IEnumerable;
    }

    private static object GetAnimWindowStateObject()
    {
        object animEditorObject = GetAnimEditorObject();

        return GetFieldObject(animEditorObject, "m_State");
    }

    private static object GetAnimEditorObject()
    {
        object openWindowObject = GetOpenAnimationWindow();

        return GetFieldObject(openWindowObject, "m_AnimEditor");
    }

    private static GameObject GetRootGameObject()
    {
        object animStateObj = GetAnimWindowStateObject();

        object selectionObj = GetPropertyObject(animStateObj, "selection");

        object rootGameObjectObj = GetPropertyObject(selectionObj, "rootGameObject");

        if (rootGameObjectObj != null)
        {
            return rootGameObjectObj as GameObject;
        }
        else
        {
            Debug.LogWarning("Failed to get value of \"rootGameObject\" property");

            return null;
        }
    }

    private static object GetFieldObject(object objToGetFrom, string fieldName, BindingFlags flags = Flags)
    {
        if (objToGetFrom != null)
        {
            try
            {
                FieldInfo field = objToGetFrom.GetType().GetField(fieldName, flags);
                object fieldObject = field.GetValue(objToGetFrom);

                return fieldObject;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to get field from object: " + e.ToString());

                return null;
            }
        }

        return null;
    }

    private static object GetPropertyObject(object objToGetFrom, string propertyName, BindingFlags flags = Flags)
    {
        if (objToGetFrom != null)
        {
            try
            {
                PropertyInfo property = objToGetFrom.GetType().GetProperty(propertyName, flags);
                object propertyObject = property.GetValue(objToGetFrom);

                return propertyObject;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to get property from object: " + e.ToString());

                return null;
            }
        }

        return null;
    }

    private static object InvokeMethod(object objToInvokeFrom, string methodName, object[] parameters)
    {
        if (objToInvokeFrom != null)
        {
            MethodInfo method = objToInvokeFrom.GetType().GetMethod(methodName);

            try
            {
                return method.Invoke(objToInvokeFrom, parameters);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to invoke method " + method + ": " + e.ToString());

                return null;
            }
        }

        return null;
    }
}
