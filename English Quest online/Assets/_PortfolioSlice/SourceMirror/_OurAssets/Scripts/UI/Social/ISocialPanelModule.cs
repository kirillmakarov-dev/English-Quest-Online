public interface ISocialPanelModule
{
    void Initialize(SocialPanelContext context);
    void Refresh();
    void OnPanelOpened();
    void OnPanelClosed();
    void Dispose();
}
