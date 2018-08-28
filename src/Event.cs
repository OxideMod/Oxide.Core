using System;
using System.Collections.Generic;

namespace Umod
{
    public class Event
    {
        public delegate void Action<in T1, in T2, in T3, in T4, in T5>(T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4);

        public static void Remove(ref Callback callback)
        {
            if (callback != null)
            {
                callback.Remove();
                callback = null;
            }
        }

        public static void Remove<T1>(ref Callback<T1> callback)
        {
            if (callback != null)
            {
                callback.Remove();
                callback = null;
            }
        }

        public static void Remove<T1, T2>(ref Callback<T1, T2> callback)
        {
            if (callback != null)
            {
                callback.Remove();
                callback = null;
            }
        }

        public static void Remove<T1, T2, T3>(ref Callback<T1, T2, T3> callback)
        {
            if (callback != null)
            {
                callback.Remove();
                callback = null;
            }
        }

        public static void Remove<T1, T2, T3, T4>(ref Callback<T1, T2, T3, T4> callback)
        {
            if (callback != null)
            {
                callback.Remove();
                callback = null;
            }
        }

        public static void Remove<T1, T2, T3, T4, T5>(ref Callback<T1, T2, T3, T4, T5> callback)
        {
            if (callback != null)
            {
                callback.Remove();
                callback = null;
            }
        }

        public class Callback
        {
            public Action Invoke;
            internal Callback Previous;
            internal Callback Next;
            internal Event Handler;

            public Callback(Action callback)
            {
                Invoke = callback;
            }

            public void Call()
            {
                Action action = Invoke;
                if (action != null)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Interface.Umod.LogException("Exception while invoking event handler", ex);
                    }
                }
            }

            public void Remove()
            {
                Event handler = Handler;
                Callback next = Next;
                Callback previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null)
                    {
                        handler.Last = previous;
                    }
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null)
                    {
                        handler.First = next;
                    }
                }
                if (handler.Invoking)
                {
                    handler.RemovedQueue.Enqueue(this);
                }
                else
                {
                    Previous = null;
                    Next = null;
                }
                Invoke = null;
                Handler = null;
            }
        }

        public class Callback<T>
        {
            public Action<T> Invoke;
            internal Callback<T> Previous;
            internal Callback<T> Next;
            internal Event<T> Handler;

            public Callback(Action<T> callback)
            {
                Invoke = callback;
            }

            public void Call(T arg0)
            {
                Action<T> action = Invoke;
                if (action != null)
                {
                    try
                    {
                        action.Invoke(arg0);
                    }
                    catch (Exception ex)
                    {
                        Interface.Umod.LogException("Exception while invoking event handler", ex);
                    }
                }
            }

            public void Remove()
            {
                Event<T> handler = Handler;
                Callback<T> next = Next;
                Callback<T> previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null)
                    {
                        handler.Last = previous;
                    }
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null)
                    {
                        handler.First = next;
                    }
                }
                if (handler.Invoking)
                {
                    handler.RemovedQueue.Enqueue(this);
                }
                else
                {
                    Previous = null;
                    Next = null;
                }
                Invoke = null;
                Handler = null;
            }
        }

        public class Callback<T1, T2>
        {
            public Action<T1, T2> Invoke;
            internal Callback<T1, T2> Previous;
            internal Callback<T1, T2> Next;
            internal Event<T1, T2> Handler;

            public Callback(Action<T1, T2> callback)
            {
                Invoke = callback;
            }

            public void Call(T1 arg0, T2 arg1)
            {
                Action<T1, T2> action = Invoke;
                if (action != null)
                {
                    try
                    {
                        action.Invoke(arg0, arg1);
                    }
                    catch (Exception ex)
                    {
                        Interface.Umod.LogException("Exception while invoking event handler", ex);
                    }
                }
            }

            public void Remove()
            {
                Event<T1, T2> handler = Handler;
                Callback<T1, T2> next = Next;
                Callback<T1, T2> previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null)
                    {
                        handler.Last = previous;
                    }
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null)
                    {
                        handler.First = next;
                    }
                }
                if (handler.Invoking)
                {
                    handler.RemovedQueue.Enqueue(this);
                }
                else
                {
                    Previous = null;
                    Next = null;
                }
                Invoke = null;
                Handler = null;
            }
        }

        public class Callback<T1, T2, T3>
        {
            public Action<T1, T2, T3> Invoke;
            internal Callback<T1, T2, T3> Previous;
            internal Callback<T1, T2, T3> Next;
            internal Event<T1, T2, T3> Handler;

            public Callback(Action<T1, T2, T3> callback)
            {
                Invoke = callback;
            }

            public void Call(T1 arg0, T2 arg1, T3 arg2)
            {
                Action<T1, T2, T3> action = Invoke;
                if (action != null)
                {
                    try
                    {
                        action.Invoke(arg0, arg1, arg2);
                    }
                    catch (Exception ex)
                    {
                        Interface.Umod.LogException("Exception while invoking event handler", ex);
                    }
                }
            }

            public void Remove()
            {
                Event<T1, T2, T3> handler = Handler;
                Callback<T1, T2, T3> next = Next;
                Callback<T1, T2, T3> previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null)
                    {
                        handler.Last = previous;
                    }
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null)
                    {
                        handler.First = next;
                    }
                }
                if (handler.Invoking)
                {
                    handler.RemovedQueue.Enqueue(this);
                }
                else
                {
                    Previous = null;
                    Next = null;
                }
                Invoke = null;
                Handler = null;
            }
        }

        public class Callback<T1, T2, T3, T4>
        {
            public Action<T1, T2, T3, T4> Invoke;
            internal Callback<T1, T2, T3, T4> Previous;
            internal Callback<T1, T2, T3, T4> Next;
            internal Event<T1, T2, T3, T4> Handler;

            public Callback(Action<T1, T2, T3, T4> callback)
            {
                Invoke = callback;
            }

            public void Call(T1 arg0, T2 arg1, T3 arg2, T4 arg3)
            {
                Action<T1, T2, T3, T4> action = Invoke;
                if (action != null)
                {
                    try
                    {
                        action.Invoke(arg0, arg1, arg2, arg3);
                    }
                    catch (Exception ex)
                    {
                        Interface.Umod.LogException("Exception while invoking event handler", ex);
                    }
                }
            }

            public void Remove()
            {
                Event<T1, T2, T3, T4> handler = Handler;
                Callback<T1, T2, T3, T4> next = Next;
                Callback<T1, T2, T3, T4> previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null)
                    {
                        handler.Last = previous;
                    }
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null)
                    {
                        handler.First = next;
                    }
                }
                if (handler.Invoking)
                {
                    handler.RemovedQueue.Enqueue(this);
                }
                else
                {
                    Previous = null;
                    Next = null;
                }
                Invoke = null;
                Handler = null;
            }
        }

        public class Callback<T1, T2, T3, T4, T5>
        {
            public Action<T1, T2, T3, T4, T5> Invoke;
            internal Callback<T1, T2, T3, T4, T5> Previous;
            internal Callback<T1, T2, T3, T4, T5> Next;
            internal Event<T1, T2, T3, T4, T5> Handler;

            public Callback(Action<T1, T2, T3, T4, T5> callback)
            {
                Invoke = callback;
            }

            public void Call(T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4)
            {
                Action<T1, T2, T3, T4, T5> action = Invoke;
                if (action != null)
                {
                    try
                    {
                        action.Invoke(arg0, arg1, arg2, arg3, arg4);
                    }
                    catch (Exception ex)
                    {
                        Interface.Umod.LogException("Exception while invoking event handler", ex);
                    }
                }
            }

            public void Remove()
            {
                Event<T1, T2, T3, T4, T5> handler = Handler;
                Callback<T1, T2, T3, T4, T5> next = Next;
                Callback<T1, T2, T3, T4, T5> previous = Previous;
                if (previous == null)
                {
                    handler.First = next;
                }
                else
                {
                    previous.Next = next;
                    if (next == null)
                    {
                        handler.Last = previous;
                    }
                }
                if (next == null)
                {
                    handler.Last = previous;
                }
                else
                {
                    next.Previous = previous;
                    if (previous == null)
                    {
                        handler.First = next;
                    }
                }
                if (handler.Invoking)
                {
                    handler.RemovedQueue.Enqueue(this);
                }
                else
                {
                    Previous = null;
                    Next = null;
                }
                Invoke = null;
                Handler = null;
            }
        }

        public Callback First;
        public Callback Last;

        internal object Lock = new object();
        internal bool Invoking;
        internal Queue<Callback> RemovedQueue = new Queue<Callback>();

        public void Add(Callback callback)
        {
            callback.Handler = this;
            lock (Lock)
            {
                Callback last = Last;
                if (last == null)
                {
                    First = callback;
                    Last = callback;
                }
                else
                {
                    last.Next = callback;
                    callback.Previous = last;
                    Last = callback;
                }
            }
        }

        public Callback Add(Action callback)
        {
            Callback eventCallback = new Callback(callback);
            Add(eventCallback);
            return eventCallback;
        }

        public void Invoke()
        {
            lock (Lock)
            {
                Invoking = true;
                Callback callback = First;
                while (callback != null)
                {
                    callback.Call();
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Callback> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }
    }

    public class Event<T>
    {
        public Event.Callback<T> First;
        public Event.Callback<T> Last;

        internal object Lock = new object();
        internal bool Invoking;
        internal Queue<Event.Callback<T>> RemovedQueue = new Queue<Event.Callback<T>>();

        public void Add(Event.Callback<T> callback)
        {
            callback.Handler = this;
            lock (Lock)
            {
                Event.Callback<T> last = Last;
                if (last == null)
                {
                    First = callback;
                    Last = callback;
                }
                else
                {
                    last.Next = callback;
                    callback.Previous = last;
                    Last = callback;
                }
            }
        }

        public Event.Callback<T> Add(Action<T> callback)
        {
            Event.Callback<T> eventCallback = new Event.Callback<T>(callback);
            Add(eventCallback);
            return eventCallback;
        }

        public void Invoke(T arg0)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0);
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }
    }

    public class Event<T1, T2>
    {
        public Event.Callback<T1, T2> First;
        public Event.Callback<T1, T2> Last;

        internal object Lock = new object();
        internal bool Invoking;
        internal Queue<Event.Callback<T1, T2>> RemovedQueue = new Queue<Event.Callback<T1, T2>>();

        public void Add(Event.Callback<T1, T2> callback)
        {
            callback.Handler = this;
            lock (Lock)
            {
                Event.Callback<T1, T2> last = Last;
                if (last == null)
                {
                    First = callback;
                    Last = callback;
                }
                else
                {
                    last.Next = callback;
                    callback.Previous = last;
                    Last = callback;
                }
            }
        }

        public Event.Callback<T1, T2> Add(Action<T1, T2> callback)
        {
            Event.Callback<T1, T2> eventCallback = new Event.Callback<T1, T2>(callback);
            Add(eventCallback);
            return eventCallback;
        }

        public void Invoke()
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2> callback = First;
                while (callback != null)
                {
                    callback.Call(default(T1), default(T2));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, default(T2));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1);
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }
    }

    public class Event<T1, T2, T3>
    {
        public Event.Callback<T1, T2, T3> First;
        public Event.Callback<T1, T2, T3> Last;

        internal object Lock = new object();
        internal bool Invoking;
        internal Queue<Event.Callback<T1, T2, T3>> RemovedQueue = new Queue<Event.Callback<T1, T2, T3>>();

        public void Add(Event.Callback<T1, T2, T3> callback)
        {
            callback.Handler = this;
            lock (Lock)
            {
                Event.Callback<T1, T2, T3> last = Last;
                if (last == null)
                {
                    First = callback;
                    Last = callback;
                }
                else
                {
                    last.Next = callback;
                    callback.Previous = last;
                    Last = callback;
                }
            }
        }

        public Event.Callback<T1, T2, T3> Add(Action<T1, T2, T3> callback)
        {
            Event.Callback<T1, T2, T3> eventCallback = new Event.Callback<T1, T2, T3>(callback);
            Add(eventCallback);
            return eventCallback;
        }

        public void Invoke()
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3> callback = First;
                while (callback != null)
                {
                    callback.Invoke(default(T1), default(T2), default(T3));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, default(T2), default(T3));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, default(T3));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, arg2);
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }
    }

    public class Event<T1, T2, T3, T4>
    {
        public Event.Callback<T1, T2, T3, T4> First;
        public Event.Callback<T1, T2, T3, T4> Last;

        internal object Lock = new object();
        internal bool Invoking;
        internal Queue<Event.Callback<T1, T2, T3, T4>> RemovedQueue = new Queue<Event.Callback<T1, T2, T3, T4>>();

        public void Add(Event.Callback<T1, T2, T3, T4> callback)
        {
            callback.Handler = this;
            lock (Lock)
            {
                Event.Callback<T1, T2, T3, T4> last = Last;
                if (last == null)
                {
                    First = callback;
                    Last = callback;
                }
                else
                {
                    last.Next = callback;
                    callback.Previous = last;
                    Last = callback;
                }
            }
        }

        public Event.Callback<T1, T2, T3, T4> Add(Action<T1, T2, T3, T4> callback)
        {
            Event.Callback<T1, T2, T3, T4> eventCallback = new Event.Callback<T1, T2, T3, T4>(callback);
            Add(eventCallback);
            return eventCallback;
        }

        public void Invoke()
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4> callback = First;
                while (callback != null)
                {
                    callback.Call(default(T1), default(T2), default(T3), default(T4));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, default(T2), default(T3), default(T4));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, default(T3), default(T4));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, arg2, default(T4));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2, T4 arg3)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, arg2, arg3);
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }
    }

    public class Event<T1, T2, T3, T4, T5>
    {
        public Event.Callback<T1, T2, T3, T4, T5> First;
        public Event.Callback<T1, T2, T3, T4, T5> Last;

        internal object Lock = new object();
        internal bool Invoking;
        internal Queue<Event.Callback<T1, T2, T3, T4, T5>> RemovedQueue = new Queue<Event.Callback<T1, T2, T3, T4, T5>>();

        public void Add(Event.Callback<T1, T2, T3, T4, T5> callback)
        {
            callback.Handler = this;
            lock (Lock)
            {
                Event.Callback<T1, T2, T3, T4, T5> last = Last;
                if (last == null)
                {
                    First = callback;
                    Last = callback;
                }
                else
                {
                    last.Next = callback;
                    callback.Previous = last;
                    Last = callback;
                }
            }
        }

        public Event.Callback<T1, T2, T3, T4, T5> Add(Event.Action<T1, T2, T3, T4, T5> callback)
        {
            Event.Callback<T1, T2, T3, T4, T5> eventCallback = new Event.Callback<T1, T2, T3, T4, T5>(callback);
            Add(eventCallback);
            return eventCallback;
        }

        public void Invoke()
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4, T5> callback = First;
                while (callback != null)
                {
                    callback.Call(default(T1), default(T2), default(T3), default(T4), default(T5));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4, T5>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4, T5> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, default(T2), default(T3), default(T4), default(T5));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4, T5>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4, T5> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, default(T3), default(T4), default(T5));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4, T5>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4, T5> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, arg2, default(T4), default(T5));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4, T5>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2, T4 arg3)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4, T5> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, arg2, arg3, default(T5));
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4, T5>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }

        public void Invoke(T1 arg0, T2 arg1, T3 arg2, T4 arg3, T5 arg4)
        {
            lock (Lock)
            {
                Invoking = true;
                Event.Callback<T1, T2, T3, T4, T5> callback = First;
                while (callback != null)
                {
                    callback.Call(arg0, arg1, arg2, arg3, arg4);
                    callback = callback.Next;
                }
                Invoking = false;
                Queue<Event.Callback<T1, T2, T3, T4, T5>> removedQueue = RemovedQueue;
                while (removedQueue.Count > 0)
                {
                    callback = removedQueue.Dequeue();
                    callback.Previous = null;
                    callback.Next = null;
                }
            }
        }
    }
}
