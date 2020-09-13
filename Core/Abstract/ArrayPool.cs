using System.Collections.Generic;

namespace UnityEngine.NetLibrary
{
    internal static class ArrayPool<T>
    {
        private static readonly Dictionary<int, Stack<T[]>> m_buffers = new Dictionary<int, Stack<T[]>>();

        private static Stack<T[]> GetStack(int p_size)
        {
            Stack<T[]> stack;
            if (!m_buffers.TryGetValue(p_size, out stack))
            {
                stack = new Stack<T[]>();
                m_buffers.Add(p_size, stack);
            }
            return stack;
        }

        internal static T[] GetBuffer(int p_size)
        {
            var stack = GetStack(p_size);

            if (stack.Count > 0)
            {
                return stack.Pop();
            }
            else
            {
                return new T[p_size];
            }
        }

        internal static void FreeBuffer(T[] p_buffer)
        {
            if (p_buffer != null)
            {
                GetStack(p_buffer.Length).Push(p_buffer);
            }
        }

    }
}
