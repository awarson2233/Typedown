using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core;
using Typedown.Core.Editor;

namespace Typedown.ArchitectureTests.Guards;

/// <summary>
/// 编辑会话契约的形状守卫：会话接口只有六个成员、命令 / 事件 / 请求全部强类型，
/// 契约不认识线上的类型名与 JSON 标注，ViewModel 只认契约、不碰线协议层，整个 Core 不再碰 Newtonsoft。
/// </summary>
[TestClass]
public sealed class EditorContractGuardTests
{
    private static readonly Assembly CoreAssembly = typeof(CoreServiceCollectionExtensions).Assembly;

    private const BindingFlags Everything =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [TestMethod]
    public void Core_MustNotReferenceNewtonsoft()
    {
        var references = CoreAssembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        CollectionAssert.DoesNotContain(references, "Newtonsoft.Json");
    }

    [TestMethod]
    public void ViewModels_MustNotUseNewtonsoftTypes()
    {
        var viewModelTypes = CoreAssembly.GetTypes()
            .Where(t => t.Namespace == "Typedown.Core.ViewModels")
            .ToList();
        Assert.IsTrue(viewModelTypes.Count > 0);

        foreach (var type in viewModelTypes)
        {
            foreach (var (used, context) in UsedTypes(type))
            {
                Assert.IsFalse(
                    IsNewtonsoft(used),
                    $"{type.FullName} uses Newtonsoft type {used.FullName} ({context}).");
            }
        }
    }

    [TestMethod]
    public void EditorSession_HasExactlySixMembers()
    {
        var members = typeof(IEditorSession).GetMembers()
            .Where(m => m is not MethodInfo { IsSpecialName: true })
            .Select(m => m.Name)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "Document", "Events", "Post", "RequestAsync", "State", "StateChanged" },
            members);
    }

    [TestMethod]
    public void EditorContract_IsStronglyTyped()
    {
        var contractTypes = CoreAssembly.GetExportedTypes()
            .Where(t => t.Namespace == "Typedown.Core.Editor"
                && (typeof(EditorCommand).IsAssignableFrom(t)
                    || typeof(EditorEvent).IsAssignableFrom(t)
                    || IsEditorRequest(t)))
            .ToList();
        Assert.IsTrue(contractTypes.Count > 0);

        foreach (var type in contractTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name != "EqualityContract"))
            {
                foreach (var used in Flatten(property.PropertyType))
                {
                    Assert.AreNotEqual(typeof(object), used, $"{type.Name}.{property.Name} is untyped.");
                    Assert.AreNotEqual(typeof(JsonElement), used, $"{type.Name}.{property.Name} leaks raw JSON.");
                    Assert.IsFalse(IsNewtonsoft(used), $"{type.Name}.{property.Name} leaks Newtonsoft.");
                }
            }
        }
    }

    [TestMethod]
    public void EditorContract_DoesNotKnowItsWireNames()
    {
        var contractTypes = CoreAssembly.GetTypes().Where(t => t.Namespace == "Typedown.Core.Editor").ToList();
        Assert.IsTrue(contractTypes.Count > 0);

        foreach (var type in contractTypes)
        {
            Assert.IsFalse(
                type.GetCustomAttributes(inherit: false).Any(a => a.GetType().Namespace == "System.Text.Json.Serialization"),
                $"{type.Name} carries JSON attributes; wire names and shapes belong to Typedown.Core.Editor.Wire.");
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var used in Flatten(property.PropertyType))
                {
                    Assert.IsFalse(IsWire(used), $"{type.Name}.{property.Name} uses wire type {used.Name}.");
                }
            }
        }
    }

    [TestMethod]
    public void ViewModels_MustNotUseWireTypes()
    {
        foreach (var type in CoreAssembly.GetTypes().Where(t => t.Namespace == "Typedown.Core.ViewModels"))
        {
            foreach (var (used, context) in UsedTypes(type))
            {
                Assert.IsFalse(IsWire(used), $"{type.FullName} uses wire type {used.FullName} ({context}); ViewModels only know the session contract.");
            }
        }
    }

    private static bool IsWire(Type type) => type.Namespace == "Typedown.Core.Editor.Wire";

    private static bool IsEditorRequest(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EditorRequest<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNewtonsoft(Type type) => type.Namespace?.StartsWith("Newtonsoft", StringComparison.Ordinal) == true;

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        if (type.HasElementType)
        {
            foreach (var inner in Flatten(type.GetElementType()!))
            {
                yield return inner;
            }
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments().SelectMany(Flatten))
            {
                yield return argument;
            }
        }
    }

    private static IEnumerable<(Type Used, string Context)> UsedTypes(Type type)
    {
        if (type.BaseType is { } baseType)
        {
            foreach (var used in Flatten(baseType)) yield return (used, "base type");
        }

        foreach (var used in type.GetInterfaces().SelectMany(Flatten)) yield return (used, "interface");

        foreach (var field in type.GetFields(Everything))
        {
            foreach (var used in Flatten(field.FieldType)) yield return (used, $"field {field.Name}");
        }

        foreach (var property in type.GetProperties(Everything))
        {
            foreach (var used in Flatten(property.PropertyType)) yield return (used, $"property {property.Name}");
        }

        foreach (var method in type.GetMethods(Everything).Cast<MethodBase>().Concat(type.GetConstructors(Everything)))
        {
            if (method is MethodInfo info)
            {
                foreach (var used in Flatten(info.ReturnType)) yield return (used, $"return of {method.Name}");
            }

            foreach (var parameter in method.GetParameters())
            {
                foreach (var used in Flatten(parameter.ParameterType)) yield return (used, $"parameter of {method.Name}");
            }

            var locals = method.GetMethodBody()?.LocalVariables ?? [];
            foreach (var local in locals)
            {
                foreach (var used in Flatten(local.LocalType)) yield return (used, $"local in {method.Name}");
            }
        }
    }
}
