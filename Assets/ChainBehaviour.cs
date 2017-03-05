using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ChainBehaviour : MonoBehaviour
{
    static ChainBehaviour instance = null;
    public static ChainBehaviour Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject();
                instance = obj.AddComponent<ChainBehaviour>();
            }
            return instance;
        }
    }
}

public interface IChain
{
    void Run(Action callback);
    void Reset(Action<Action> actionWithCallback);
    void ResetSerial(IEnumerator<Chain> chains);
    void ResetParallel(IEnumerable<Chain> chains);
}

public class Chain : IChain
{
    Action<Action> actionWithCallback;

    #region single

    public static Chain Create(Action action)
    {
        return Create(callback =>
        {
            action.Invoke();
            callback.Invoke();
        });
    }

    public static Chain Create(Action<Action> actionWithCallback)
    {
        return new Chain(actionWithCallback);
    }

    public static Chain Create(IEnumerator enumerator)
    {
        return Create(callback =>
        {
            ChainBehaviour.Instance.StartCoroutine(GetEnumeratorWithCallback(enumerator, callback));
        });
    }

    static IEnumerator GetEnumeratorWithCallback(IEnumerator enumerator, Action callback)
    {
        yield return enumerator;
        callback.Invoke();
    }

    Chain(Action<Action> actionWithCallback)
    {
        (this as IChain).Reset(actionWithCallback);
    }

    void IChain.Reset(Action<Action> actionWithCallback)
    {
        this.actionWithCallback = actionWithCallback;
    }

    #endregion

    #region serial

    public static Chain Serial(params Action[] actions)
    {
        return Serial(actions as IEnumerable<Action>);
    }

    public static Chain Serial(params Action<Action>[] actionsWithCallback)
    {
        return Serial(actionsWithCallback as IEnumerable<Action<Action>>);
    }

    public static Chain Serial(params IEnumerator[] enumerables)
    {
        return Serial(enumerables as IEnumerable<IEnumerator>);
    }

    public static Chain Serial(params Chain[] chains)
    {
        return Serial(chains as IEnumerable<Chain>);
    }

    public static Chain Serial(IEnumerable<Action> actions)
    {
        return Serial(actions.Select(action => Create(action)));
    }

    public static Chain Serial(IEnumerable<Action<Action>> actionsWithCallback)
    {
        return Serial(actionsWithCallback.Select(actionWithCallback => Create(actionWithCallback)));
    }

    public static Chain Serial(IEnumerable<IEnumerator> enumerables)
    {
        return Serial(enumerables.Select(enumerator => Create(enumerator)));
    }

    public static Chain Serial(IEnumerable<Chain> chains)
    {
        return Serial(chains.GetEnumerator());
    }

    static Chain Serial(IEnumerator<Chain> chains)
    {
        return Get(
            () => new Chain(chains),
            chain => (chain as IChain).ResetSerial(chains)
        );
    }

    Chain(IEnumerator<Chain> chains)
    {
        ResetSerial(chains);
    }

    void IChain.ResetSerial(IEnumerator<Chain> chains)
    {
        ResetSerial(chains);
    }

    void ResetSerial(IEnumerator<Chain> chains)
    {
        this.actionWithCallback = callback =>
        {
            bool moveNext = chains.MoveNext();

            if (moveNext)
                (chains.Current as IChain).Run(() => (Serial(chains) as IChain).Run(callback));
            else
                callback.Invoke();
        };
    }

    #endregion

    #region parallel

    public static Chain Parallel(params Action[] actions)
    {
        return Parallel(actions as IEnumerable<Action>);
    }

    public static Chain Parallel(params Action<Action>[] actionsWithCallback)
    {
        return Parallel(actionsWithCallback as IEnumerable<Action<Action>>);
    }

    public static Chain Parallel(params IEnumerator[] enumerables)
    {
        return Parallel(enumerables as IEnumerable<IEnumerator>);
    }

    public static Chain Parallel(params Chain[] chains)
    {
        return Parallel(chains as IEnumerable<Chain>);
    }

    public static Chain Parallel(IEnumerable<Action> actions)
    {
        return Parallel(actions.Select(action => Create(action)));
    }

    public static Chain Parallel(IEnumerable<Action<Action>> actionsWithCallback)
    {
        return Parallel(actionsWithCallback.Select(actionWithCallback => Create(actionWithCallback)));
    }

    public static Chain Parallel(IEnumerable<IEnumerator> enumerables)
    {
        return Parallel(enumerables.Select(enumerator => Create(enumerator)));
    }

    public static Chain Parallel(IEnumerable<Chain> chains)
    {
        return Get(
            () => new Chain(chains, true),
            chain => (chain as IChain).ResetParallel(chains)
        );
    }

    Chain(IEnumerable<Chain> chains, bool parallel = true)
    {
        if (parallel)
            (this as IChain).ResetParallel(chains);
        else
            (this as IChain).ResetSerial(chains.GetEnumerator());
    }

    void IChain.ResetParallel(IEnumerable<Chain> chains)
    {
        this.actionWithCallback = callback =>
        {
            int remain = 1;
            Action onFinish = () =>
            {
                remain--;
                if (remain == 0)
                    callback.Invoke();
            };

            foreach (var chain in chains)
            {
                remain++;
                (chain as IChain).Run(onFinish);
            }

            onFinish.Invoke();
        };
    }

    #endregion

    #region run

    public void Run()
    {
        (this as IChain).Run(() => { });
    }

    void IChain.Run(Action callback)
    {
        if (actionWithCallback != null)
        {
            actionWithCallback.Invoke(callback);
            actionWithCallback = null;
            chainPool.Enqueue(this);
        }
        else
        {
            Debug.LogError("cannot run again");
        }
    }

    #endregion

    #region pool

    static Queue<Chain> chainPool = new Queue<Chain>();

    static Chain Get(Func<Chain> createAction, Action<Chain> resetAction)
    {
        if (chainPool.Count > 0)
        {
            var chain = chainPool.Dequeue();
            resetAction.Invoke(chain);
            return chain;
        }
        else
        {
            return createAction.Invoke();
        }
    }

    #endregion
}
