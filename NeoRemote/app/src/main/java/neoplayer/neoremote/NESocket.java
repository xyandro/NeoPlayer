package neoplayer.neoremote;

import android.bluetooth.BluetoothSocket;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;

public class NESocket {
    BluetoothSocket btSocket;
    Socket ipSocket;

    public NESocket(BluetoothSocket socket) {
        this.btSocket = socket;
    }

    public NESocket(Socket socket) {
        this.ipSocket = socket;
    }

    public OutputStream getOutputStream() throws IOException {
        if (btSocket != null)
            return btSocket.getOutputStream();
        return ipSocket.getOutputStream();
    }

    public InputStream getInputStream() throws IOException {
        if (btSocket != null)
            return btSocket.getInputStream();
        return ipSocket.getInputStream();
    }

    public void close() throws IOException {
        if (btSocket != null)
            btSocket.close();
        ipSocket.close();
    }

    public boolean isClosed() {
        if (btSocket != null)
            return false;
        return ipSocket.isClosed();
    }
}
