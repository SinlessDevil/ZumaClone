using System;

namespace Code.Infrastructure.Services.SaveLoad
{
    [Serializable]
    public enum SaveMethod
    {
        PlayerPrefs,
        Json,
        Xml
    }
}