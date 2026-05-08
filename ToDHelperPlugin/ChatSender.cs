using System;
using System.Text;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace ToDHelperPlugin;

public unsafe class ChatSender
{
    private static void SendMessageUnsafe(byte[] message)
    {
        var mes = Utf8String.FromSequence(message.NullTerminate());
        UIModule.Instance()->ProcessChatBoxEntry(mes);
        mes->Dtor(true);
    }

    public static void SendMessage(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        if (bytes.Length == 0) return;

        // Protective cap to prevent buffer overrun crashes in internal FFXIV functions
        if (bytes.Length > 500)
            throw new ArgumentException("message is longer than 500 bytes", nameof(message));

        SendMessageUnsafe(bytes);
    }
}
