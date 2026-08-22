namespace Frontend.App.Services;

/// <summary>
/// Chia sẻ trạng thái thanh công cụ bài thi (nút nộp bài + đồng hồ)
/// giữa các trang thi và navbar chính trong layout.
/// </summary>
public class ExamHeaderService
{
    public bool Visible { get; private set; }
    public string TimerText { get; private set; } = "";
    public bool TimerUrgent { get; private set; }

    /// <summary>Bắn ra khi trạng thái thay đổi để layout re-render.</summary>
    public event Action? OnChange;

    /// <summary>Bắn ra khi người dùng bấm nút Nộp bài trên navbar.</summary>
    public event Action? SubmitRequested;

    public void Show(string timerText)
    {
        Visible = true;
        TimerText = timerText;
        TimerUrgent = false;
        Notify();
    }

    public void UpdateTimer(string text, bool urgent)
    {
        if (!Visible) return;
        var changed = TimerText != text || TimerUrgent != urgent;
        TimerText = text;
        TimerUrgent = urgent;
        if (changed) Notify();
    }

    public void Hide()
    {
        if (!Visible && string.IsNullOrEmpty(TimerText)) return;
        Visible = false;
        TimerUrgent = false;
        Notify();
    }

    public void RequestSubmit() => SubmitRequested?.Invoke();

    private void Notify() => OnChange?.Invoke();
}
