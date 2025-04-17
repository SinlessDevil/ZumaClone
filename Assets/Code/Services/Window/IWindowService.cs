using Code.Window;
using UnityEngine;

namespace Code.Services.Window
{
    public interface IWindowService
    {
        RectTransform Open(WindowTypeId windowTypeId);
    }
}