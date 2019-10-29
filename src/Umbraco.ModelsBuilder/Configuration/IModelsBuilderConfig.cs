﻿namespace Umbraco.ModelsBuilder.Configuration
{
    public interface IModelsBuilderConfig
    {
        bool AcceptUnsafeModelsDirectory { get; }
        int DebugLevel { get; }
        bool EnableFactory { get; }
        bool FlagOutOfDateModels { get; }
        bool IsDebug { get; }
        string ModelsDirectory { get; }
        ModelsMode ModelsMode { get; }
        string ModelsNamespace { get; }
    }
}
