public interface  IHealer
{
    bool IsHealing { get; }
    void StartHeal();   
    void CancelHealing();
}
