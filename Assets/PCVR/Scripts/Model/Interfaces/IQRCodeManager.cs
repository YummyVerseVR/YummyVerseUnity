using System;
using R3;

namespace PCVR.Model.Interfaces
{

    public interface IQRCodeManager
    {
        ReactiveProperty<Guid> UserId { get; }
    }
}