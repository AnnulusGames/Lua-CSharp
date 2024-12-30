using System;
using Lua;
using Lua.Loaders;
using Lua.Standard;
using Lua.Unity;
using UnityEngine;

public class Sandbox : MonoBehaviour
{
    async void Start()
    {
        var state = LuaState.Create();
        state.ModuleLoader = CompositeModuleLoader.Create(new AddressablesModuleLoader(), new ResourcesModuleLoader());
        state.OpenStandardLibraries();
        state.Environment["print"] = new LuaFunction("print", (context, buffer, ct) =>
        {
            Debug.Log(context.GetArgument<string>(0));
            return new(0);
        });

        try
        {
            await state.DoStringAsync(
    @"
print('test start')
local foo = require 'foo'
foo.greet()

local bar = require 'bar'
bar.greet()

", cancellationToken: destroyCancellationToken);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}