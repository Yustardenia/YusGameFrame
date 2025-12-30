using System.Reflection;
using Fungus;
using UnityEngine;

[CommandInfo("YusVN", "Say (Unskippable)", "Say text that cannot be skipped by clicking while typing.")]
public class SayUnskippable : Say
{
    private static readonly FieldInfo ClickModeField =
        typeof(DialogInput).GetField("clickMode", BindingFlags.NonPublic | BindingFlags.Instance);

    private DialogInput dialogInput;
    private ClickMode originalClickMode;
    private Writer activeWriter;
    private bool restored;

    public override void OnEnter()
    {
        restored = false;

        var sayDialog = SayDialog.GetSayDialog();
        dialogInput = sayDialog != null ? sayDialog.GetComponent<DialogInput>() : null;
        activeWriter = sayDialog != null ? sayDialog.GetComponentInChildren<Writer>() : null;

        if (dialogInput != null && ClickModeField != null)
        {
            var value = ClickModeField.GetValue(dialogInput);
            if (value is ClickMode mode) originalClickMode = mode;
            ClickModeField.SetValue(dialogInput, ClickMode.Disabled);
        }

        WriterSignals.OnWriterState += OnWriterState;

        base.OnEnter();
    }

    public override void OnExit()
    {
        RestoreIfNeeded();
        WriterSignals.OnWriterState -= OnWriterState;
        activeWriter = null;
        dialogInput = null;
        base.OnExit();
    }

    private void OnDestroy()
    {
        WriterSignals.OnWriterState -= OnWriterState;
        RestoreIfNeeded();
    }

    private void OnWriterState(Writer writer, WriterState writerState)
    {
        if (!IsExecuting) return;
        if (restored) return;
        if (writer == null || activeWriter == null) return;
        if (writer != activeWriter) return;

        if (writerState == WriterState.Pause)
        {
            RestoreIfNeeded();
        }
    }

    private void RestoreIfNeeded()
    {
        if (restored) return;
        restored = true;

        if (dialogInput == null || ClickModeField == null) return;
        ClickModeField.SetValue(dialogInput, originalClickMode);
    }

    public override string GetSummary()
    {
        return "<b>UNSKIPPABLE:</b> " + base.GetSummary();
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 200, 200, 255);
    }
}

