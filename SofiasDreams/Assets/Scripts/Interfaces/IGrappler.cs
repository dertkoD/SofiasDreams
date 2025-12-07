public interface IGrappler 
{
    bool IsGrappling { get; }
    void CancelGrapple();
}