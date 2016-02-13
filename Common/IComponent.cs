using System;
using System.Collections.Generic;
namespace Shared
{
    public interface IComponent
    {
        void AddComponent(IComponent c);
        void ClearComponents();
        void Deserialize(byte[] data, Pointer p, bool includeSubComponents);
        List<IComponent> GetAllComponents();
        T GetComponent<T>() where T : IComponent;
        IEnumerator<IComponent> GetComponentEnumerator();
        List<T> GetComponentsOfType<T>() where T : IComponent;
        string ComponentName { get; set; }
        void RemoveComponent(IComponent c);
        void Serialize(ref byte[] buffer, Pointer p, bool includeSubComponents);
        uint TypeHash { get; }
    }
}
