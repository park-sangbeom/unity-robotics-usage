using UnityEngine; 
using System.Threading; 
using System.Threading.Tasks; 

public class ParallelForExample : MonoBehaviour 
{ 
    void Start() 
    { 
        Parallel.For(0, 10, (i) => { 
            Say(i);
        }); 
    }

    private void Say(int i)
    {
        Debug.Log("Say");
        Debug.Log($"{Thread.CurrentThread.ManagedThreadId}: {i}");     
    } 
}

