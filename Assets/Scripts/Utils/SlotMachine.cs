using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable
public class SlotMachine<T> : IEnumerable<(int index, T item)> where T : class
{
    private List<T?> slots = new List<T?>();
    private Queue<int> freedSlots = new Queue<int>();

    public int Allocate(T item)
    {
        if (freedSlots.Count > 0)
        {
            int index = freedSlots.Dequeue();
            slots[index] = item;
            return index;
        }
        else
        {
            slots.Add(item);
            return slots.Count - 1;
        }
    }

    public void Free(int index)
    {
        if (index < 0 || index >= slots.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Invalid slot index.");

        if (slots[index] != null)
        {
            slots[index] = null;
            freedSlots.Enqueue(index);
        }
    }

    public T? Get(int index)
    {
        if (index < 0 || index >= slots.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Invalid slot index.");

        return slots[index];
    }

    public int Count => slots.Count - freedSlots.Count;

    public void Clear()
    {
        slots.Clear();
        freedSlots.Clear();
    }

    public IEnumerator<(int index, T item)> GetEnumerator()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                yield return (i, slots[i]!);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T? Find(Predicate<T> match)
    {
        foreach (var (_, item) in this)
        {
            if (match(item))
                return item;
        }
        return null;
    }

    public int FindIndex(Predicate<T> match)
    {
        foreach (var (index, item) in this)
        {
            if (match(item))
                return index;
        }
        return -1;
    }

    public bool Remove(T item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && EqualityComparer<T>.Default.Equals(slots[i]!, item))
            {
                Free(i);
                return true;
            }
        }
        return false;
    }

    public int RemoveWhere(Predicate<T> match)
    {
        int removed = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && match(slots[i]!))
            {
                Free(i);
                removed++;
            }
        }
        return removed;
    }
}
#nullable disable
