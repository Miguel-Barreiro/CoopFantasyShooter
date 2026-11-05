using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fullscreen.EditorPro2.Integrations.GameCreator2
{
    [InitializeOnLoad]
    internal static class GameCreatorIconIntegration
    {
        private static readonly (string TypeName, string AssemblyName, string MethodName)[] SpriteProviders =
        {
            ("GameCreator.Runtime.Shooter.ShooterWeapon", "GameCreator.Runtime.Shooter", "GetSprite"),
            ("GameCreator.Runtime.Shooter.Reload", "GameCreator.Runtime.Shooter", "GetSprite"),
            ("GameCreator.Runtime.Shooter.Ammo", "GameCreator.Runtime.Shooter", "GetSprite"),
            ("GameCreator.Runtime.Stats.Stat", "GameCreator.Runtime.Stats", "GetIcon"),
            ("GameCreator.Runtime.Stats.StatusEffect", "GameCreator.Runtime.Stats", "GetIcon"),
            ("GameCreator.Runtime.Quests.Quest", "GameCreator.Runtime.Quests", "GetSprite"),
            ("GameCreator.Runtime.Stats.Class", "GameCreator.Runtime.Stats", "GetSprite"),
            ("GameCreator.Runtime.Stats.Attribute", "GameCreator.Runtime.Stats", "GetIcon"),
            ("GameCreator.Runtime.Melee.MeleeWeapon", "GameCreator.Runtime.Melee", "GetSprite"),
            ("GameCreator.Runtime.Melee.Skill", "GameCreator.Runtime.Melee", "GetSprite")
        };

        static GameCreatorIconIntegration()
        {
            RegisterIconProviders();
        }

        private static void RegisterIconProviders()
        {
            var itemType = ResolveType("GameCreator.Runtime.Inventory.Item", "GameCreator.Runtime.Inventory");
            if (itemType != null)
                Registry.RegisterIconProvider(itemType, GetInventoryItemIcon);

            foreach (var provider in SpriteProviders)
            {
                var type = ResolveType(provider.TypeName, provider.AssemblyName);
                if (type == null) continue;

                var method = FindIconMethod(type, provider.MethodName);
                if (method == null) continue;

                Registry.RegisterIconProvider(type, asset => InvokeIconMethod(asset, method));
            }
        }

        private static Texture2D GetInventoryItemIcon(ScriptableObject asset)
        {
            if (asset == null) return null;

            try
            {
                var info = asset.GetType().GetProperty("Info", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(asset);
                if (info == null) return null;

                var method = FindIconMethod(info.GetType(), "Sprite");
                return method == null ? null : InvokeIconMethod(info, method);
            }
            catch (NullReferenceException)
            {
                return null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        private static MethodInfo FindIconMethod(Type type, string methodName)
        {
            return type?.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.Name == methodName && method.ReturnType == typeof(Sprite))
                .OrderBy(method => method.GetParameters().Length)
                .FirstOrDefault();
        }

        private static Texture2D InvokeIconMethod(object target, MethodInfo method)
        {
            if (target == null || method == null) return null;

            try
            {
                var parameters = method.GetParameters();
                var arguments = new object[parameters.Length];
                for (var index = 0; index < parameters.Length; index++)
                    arguments[index] = GetDefaultArgument(parameters[index].ParameterType);

                var result = method.Invoke(target, arguments);
                return result is Sprite sprite ? GetSpriteTexture(sprite) : result as Texture2D;
            }
            catch (NullReferenceException)
            {
                return null;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is NullReferenceException)
            {
                return null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        private static Texture2D GetSpriteTexture(Sprite sprite)
        {
            if (sprite == null) return null;

            try
            {
                return sprite.texture;
            }
            catch (NullReferenceException)
            {
                // Game Creator can return a sprite that Unity has already invalidated.
                return null;
            }
        }

        private static object GetDefaultArgument(Type type)
        {
            if (type == null) return null;
            if (!type.IsValueType)
            {
                var emptyField = type.GetField("EMPTY", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (emptyField != null) return emptyField.GetValue(null);

                var emptyProperty = type.GetProperty("EMPTY", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (emptyProperty != null) return emptyProperty.GetValue(null);
            }

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static Type ResolveType(string fullName, string assemblyName)
        {
            var type = Type.GetType($"{fullName}, {assemblyName}", false);
            if (type != null) return type;

            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);
        }
    }
}
