namespace YusGameFrame
{
    public readonly struct CommandRegistryInfo
    {
        public string Key { get; }
        public bool IsAsync { get; }
        public string ArgTypeFullName { get; }

        public CommandRegistryInfo(string key, bool isAsync, string argTypeFullName)
        {
            Key = key;
            IsAsync = isAsync;
            ArgTypeFullName = argTypeFullName;
        }

        public override string ToString()
        {
            var kind = IsAsync ? "Async" : "Sync";
            var arg = string.IsNullOrEmpty(ArgTypeFullName) ? "()" : $"({ArgTypeFullName})";
            return $"{Key} {kind} {arg}";
        }
    }
}

