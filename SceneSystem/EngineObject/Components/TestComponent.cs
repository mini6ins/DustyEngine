using DustyEngine;

public class TestComponent : MonoBehaviour
{
    public int TestNumber { get; set; }
    public string TestString { get; set; }
    private int i = 0;
    [SerializeField] private int b = 0;
    private DateTime lastUpdateTime;
    private DateTime lastFixedUpdateTime;

    public void OnEnable()
    {
        lastUpdateTime = DateTime.Now;
        lastFixedUpdateTime = DateTime.Now;
    }

    public void OnDisable()
    {
    }

    public void OnDestroy()
    {
    }

    public void Start()
    {
        //  GetComponent<Player>().Test();
        //  Console.WriteLine(gameObject.Name);


        foreach (var parentComponent in Parent.Components)
        {
            //   Debug.Log(parentComponent.GetType().Name);
        }

        // Console.WriteLine(Parent.GetComponent<Transform>());

        //  Parent.GetComponent<Player>().SetActive(false);
        //Parent.GetComponent<Player>().Parent.SetActive(false);
    }

    public void Update()
    {
        TimeSpan timeSinceLastUpdate = DateTime.Now - lastUpdateTime;

        lastUpdateTime = DateTime.Now;
        i++;
            //  Console.WriteLine($"Execute Update on: {Parent.Name} {GetType().Name} {i} (Time since last update: {timeSinceLastUpdate.TotalMilliseconds:F2} ms)");
    }

    public void FixedUpdate()
    {
        TimeSpan timeSinceLastFixedUpdate = DateTime.Now - lastFixedUpdateTime;

        lastFixedUpdateTime = DateTime.Now;
        b++;
        // Console.WriteLine(
        //   $"Execute FixedUpdate on: {Parent.Name} {GetType().Name} {b} (Time since last fixed update: {timeSinceLastFixedUpdate.TotalMilliseconds:F2} ms)");
    }
}