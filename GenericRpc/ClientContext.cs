using System;

namespace GenericRpc
{
    public sealed class ClientContext
    {
        public readonly Guid Id;
        public bool IsAccessGranted { get; set; }

        public ClientContext(Guid id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            var context = obj as ClientContext;
            if (context == null)
                return false;

            return Id == context.Id;
        }

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"{{{nameof(Id)}: {Id}, {nameof(IsAccessGranted)}: {IsAccessGranted}}}";
    }
}
