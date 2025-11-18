using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;

/// <summary>
/// 네트워크 스트림을 '한 줄(개행 \n)' 단위로 읽고 쓰는 간단 유틸.
/// - 텍스트 기반 프로토콜을 쉽게 다루기 위한 버퍼.
///
/// 사용법:
///   LineProtocol lp = new LineProtocol(stream);
///   lp.WriteLine("JOIN|Alice");
///   List<string> lines = lp.ReadAvailableLines(); // DataAvailable일 때만 호출
/// </summary>
public class LineProtocol
{
    private NetworkStream stream;                  // TCP 네트워크 스트림
    private byte[] readBuffer;                     // 수신 버퍼(고정 크기)
    private StringBuilder incoming;                // 수신 중 문자열 누적 버퍼
    private Encoding encoding;                     // 문자열 인코딩(UTF8)

    public LineProtocol(NetworkStream s)
    {
        stream = s;
        readBuffer = new byte[4096];
        incoming = new StringBuilder();
        encoding = Encoding.UTF8;
    }

    /// <summary>
    /// 개행(\n)으로 끝나는 한 줄을 전송한다.
    /// </summary>
    public void WriteLine(string line)
    {
        if (stream == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(line) == true)
        {
            return;
        }

        string withNewline = line + "\n";
        byte[] data = encoding.GetBytes(withNewline);

        try
        {
            stream.Write(data, 0, data.Length);
        }
        catch (IOException)
        {
            // 연결이 끊겼을 수 있다. 상위에서 처리.
        }
        catch (ObjectDisposedException)
        {
            // 스트림이 이미 닫혔다.
        }
    }

    /// <summary>
    /// 스트림에 도착해 있는 모든 바이트를 읽어, 줄 단위로 반환한다.
    /// - blocking 없이 NetworkStream.DataAvailable 일 때만 호출 권장.
    /// </summary>
    public List<string> ReadAvailableLines()
    {
        List<string> result = new List<string>();

        if (stream == null)
        {
            return result;
        }

        // 읽을 데이터가 없으면 바로 반환
        bool available = stream.DataAvailable;
        if (available == false)
        {
            return result;
        }

        try
        {
            while (stream.DataAvailable == true)
            {
                int read = stream.Read(readBuffer, 0, readBuffer.Length);
                if (read <= 0)
                {
                    break;
                }

                string text = encoding.GetString(readBuffer, 0, read);
                incoming.Append(text);

                // 누적 버퍼에서 줄 단위로 분리
                while (true)
                {
                    string current = incoming.ToString();
                    int idx = current.IndexOf('\n');
                    if (idx < 0)
                    {
                        break;
                    }

                    string line = current.Substring(0, idx);
                    result.Add(line.Trim('\r'));
                    string rest = current.Substring(idx + 1);
                    incoming.Clear();
                    incoming.Append(rest);
                }
            }
        }
        catch (IOException)
        {
            // 연결 오류 시 상위에서 소켓 정리
        }
        catch (ObjectDisposedException)
        {
            // 이미 닫힘
        }

        return result;
    }
}
