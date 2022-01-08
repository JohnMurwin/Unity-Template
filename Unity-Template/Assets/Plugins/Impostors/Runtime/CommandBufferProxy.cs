using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Impostors
{
    public sealed class CommandBufferProxy
    {
        private readonly List<Keyword> _keywords;
        private bool _invertCulling;

        public CommandBufferProxy()
        {
            CommandBuffer = new CommandBuffer();
            _keywords = new List<Keyword>(4);
            _keywords.Add(new Keyword("LIGHTMAP_ON"));
            _keywords.Add(new Keyword("LIGHTPROBE_SH"));
            _keywords.Add(new Keyword("SHADOWS_SHADOWMASK"));
            _keywords.Add(new Keyword("DIRLIGHTMAP_COMBINED"));
            Clear();
        }

        public CommandBuffer CommandBuffer { get; }

        public void EnableShaderKeyword(int id)
        {
            var keyword = _keywords[id];
            if (keyword.State != KeywordState.Enabled)
            {
                CommandBuffer.EnableShaderKeyword(keyword.Name);
                keyword.State = KeywordState.Enabled;
            }
        }

        public void DisableShaderKeyword(int id)
        {
            var keyword = _keywords[id];
            if (keyword.State != KeywordState.Disabled)
            {
                CommandBuffer.DisableShaderKeyword(keyword.Name);
                keyword.State = KeywordState.Disabled;
            }
        }

        public int GetOrRegisterKeywordId(string keywordName)
        {
            for (int i = 0; i < _keywords.Count; i++)
            {
                if (_keywords[i].Name == keywordName)
                    return i;
            }

            _keywords.Add(new Keyword(keywordName));
            return _keywords.Count - 1;
        }

        public void SetInvertCulling(bool value)
        {
            if (_invertCulling != value)
            {
                _invertCulling = value;
                CommandBuffer.SetInvertCulling(value);
            }
        }

        public void Clear()
        {
            CommandBuffer.Clear();
            for (int i = 0; i < _keywords.Count; i++)
            {
                _keywords[i].State = KeywordState.Unknown;
            }

            _invertCulling = false;
            CommandBuffer.SetInvertCulling(false);
        }

        public void Dispose()
        {
            CommandBuffer.Dispose();
            _keywords.Clear();
        }

        private class Keyword
        {
            public readonly string Name;
            public KeywordState State;

            public Keyword(string name)
            {
                Name = name;
                State = KeywordState.Unknown;
            }
        }

        private enum KeywordState
        {
            Unknown,
            Disabled,
            Enabled
        }
    }
}