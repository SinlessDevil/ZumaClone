using System;

namespace Code.Services.SaveLoad
{
    [Serializable]
    public enum SaveMethodType
    {
        PlayerPrefs,
        Json,
        Xml
    }
}