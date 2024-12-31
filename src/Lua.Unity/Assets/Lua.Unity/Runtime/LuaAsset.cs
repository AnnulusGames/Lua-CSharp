using UnityEngine;

namespace Lua.Unity
{
    public sealed class LuaAsset : ScriptableObject
    {
        [SerializeField] internal string text;
        public string Text => text;
    }
}