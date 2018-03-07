package neoplayer.neoremote;

import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Binder;
import android.os.IBinder;
import android.support.annotation.Nullable;
import android.support.v4.content.LocalBroadcastManager;
import android.util.Log;

import java.io.InputStream;
import java.io.OutputStream;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.InterfaceAddress;
import java.net.NetworkInterface;
import java.net.Socket;
import java.net.URI;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.charset.StandardCharsets;
import java.util.Enumeration;
import java.util.concurrent.ArrayBlockingQueue;

public class NetworkService extends Service {
    public static final String ServiceIntent = "NeoPlayerNetworkService";
    private static final String TAG = NetworkService.class.getSimpleName();
    private static final String FakeProtocol = "ne://";
    private static final int NeoPlayerToken = 0xfeedbeef;
    private static final int NeoPlayerRestartToken = 0x0badf00d;
    private static final int NeoPlayerDefaultPort = 7399;
    private static final String addressFileName = "NeoPlayer.txt";

    private ArrayBlockingQueue<byte[]> outputQueue = new ArrayBlockingQueue<>(100);
    private URI neoPlayerAddress;
    private Thread networkThread;
    private Socket socket;

    public class NetworkServiceBinder extends Binder {
        public NetworkService getService() {
            return NetworkService.this;
        }
    }

    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return new NetworkServiceBinder();
    }

    @Override
    public void onCreate() {
        super.onCreate();

        networkThread = new Thread(new Runnable() {
            @Override
            public void run() {
                runNetworkThread();
            }
        });
        networkThread.start();
    }

    private void getNeoPlayerAddress() {
        try {
            InputStream in = openFileInput(addressFileName);
            try {
                byte[] buffer = new byte[4];
                in.read(buffer, 0, 4);
                int size = ByteBuffer.wrap(buffer).order(ByteOrder.LITTLE_ENDIAN).getInt();
                buffer = new byte[size];
                in.read(buffer, 0, size);
                String address = new String(buffer, "UTF-8");
                neoPlayerAddress = new URI(FakeProtocol + address);
            } finally {
                in.close();
            }
        } catch (Exception ex) {
        }
    }

    public void setNeoPlayerAddress(final String address) {
        try {
            neoPlayerAddress = new AsyncTask<Void, Void, URI>() {
                @Override
                protected URI doInBackground(Void... voids) {
                    try {
                        URI uri = new URI(FakeProtocol + address);
                        String address = InetAddress.getByName(uri.getHost()).getHostAddress();
                        int port = uri.getPort();
                        if (port == -1)
                            port = NeoPlayerDefaultPort;
                        uri = new URI(FakeProtocol + address + ":" + port);
                        return uri;
                    } catch (Exception ex) {
                        return null;
                    }
                }
            }.execute().get();
            if (neoPlayerAddress == null)
                return;

            byte[] buffer = neoPlayerAddress.toString().substring(FakeProtocol.length()).getBytes("UTF-8");
            byte[] bufferSize = ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(buffer.length).array();
            OutputStream out = openFileOutput(addressFileName, Context.MODE_PRIVATE);
            out.write(bufferSize);
            out.write(buffer);
            out.close();
        } catch (Exception ex) {
        }
    }

    private void runNetworkThread() {
        getNeoPlayerAddress();

        long startTime = System.nanoTime();
        long connectTime = 0;
        Log.d(TAG, "startNetworkThread: Started");
        while (networkThread != null) try {
            connectTime = System.nanoTime();

            if (neoPlayerAddress == null)
                throw new Exception("startNetworkThread: No connection address");

            Log.d(TAG, "startNetworkThread: Connect to " + neoPlayerAddress);

            socket = new Socket();
            socket.connect(new InetSocketAddress(neoPlayerAddress.getHost(), neoPlayerAddress.getPort()), 1000);

            try {
                Log.d(TAG, "startNetworkThread: Connected");

                {
                    Intent intent = new Intent(ServiceIntent);
                    intent.putExtra("action", "clearGetAddressDialog");
                    LocalBroadcastManager.getInstance(this).sendBroadcast(intent);
                }

                outputQueue.clear();
                outputQueue.add("GET /RunNeoRemote HTTP/1.1\r\n\r\n".getBytes(StandardCharsets.US_ASCII));

                final Thread writerThread = new Thread(new Runnable() {
                    @Override
                    public void run() {
                        runWriterThread(socket);
                    }
                });
                writerThread.start();

                try {
                    while (true) {
                        final Message message = new Message(socket.getInputStream());

                        Intent intent = new Intent(ServiceIntent);
                        intent.putExtra("action", "handleMessage");
                        intent.putExtra("message", message.getBytes());
                        LocalBroadcastManager.getInstance(this).sendBroadcast(intent);
                    }
                } finally {
                    socket.close();
                    outputQueue.add(new byte[0]); // Signal writer thread to stop
                    try {
                        writerThread.join();
                    } catch (Exception e) {
                    }
                }
            } finally {
                socket.close();
            }
        } catch (Exception ex) {
            Log.d(TAG, "startNetworkThread: Error: " + ex.getMessage());

            try {
                long sleepTime = Math.max(1000000000 - System.nanoTime() + connectTime, 0);
                Thread.sleep((int) (sleepTime / 1000000), (int) (sleepTime % 1000000));
            } catch (Exception ex2) {
            }

            if (System.nanoTime() - startTime >= 1000000000) {
                Intent intent = new Intent(ServiceIntent);
                intent.putExtra("action", "showGetAddressDialog");
                intent.putExtra("address", neoPlayerAddress == null ? "" : neoPlayerAddress.toString().substring(FakeProtocol.length()));
                LocalBroadcastManager.getInstance(this).sendBroadcast(intent);
            }
        }
        Log.d(TAG, "startNetworkThread: Stopped");
    }

    private void runWriterThread(Socket socket) {
        try {
            Log.d(TAG, "runWriterThread: Started");
            while (!socket.isClosed()) {
                byte[] message = outputQueue.take();
                if (message.length == 0)
                    continue;

                socket.getOutputStream().write(message);
            }
            Log.d(TAG, "runWriterThread: Socket disconnected");
        } catch (Exception ex) {
            Log.d(TAG, "runWriterThread: Error: " + ex.getMessage());
        }
        Log.d(TAG, "runWriterThread: Stopped");
    }

    public void sendMessage(byte[] message) {
        outputQueue.add(message);
    }

    public void sendRestart() {
        new AsyncTask<Void, Void, Void>() {
            @Override
            protected Void doInBackground(Void... voids) {
                try {
                    if (neoPlayerAddress != null)
                        new Socket(neoPlayerAddress.getHost(), neoPlayerAddress.getPort() - 1).getOutputStream().write(ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(NeoPlayerRestartToken).array());
                } catch (Exception e) {
                }
                return null;
            }
        }.execute();
    }

    @Override
    public void onDestroy() {
        super.onDestroy();

        Thread networkThread = this.networkThread;
        this.networkThread = null;
        Socket socket = this.socket;
        if (socket != null)
            try {
                socket.close();
            } catch (Exception e) {
            }
        try {
            networkThread.join();
        } catch (Exception e) {
        }
    }

    public static String findNeoPlayer() {
        try {
            return new AsyncTask<Void, Void, String>() {
                @Override
                protected String doInBackground(Void... voids) {
                    try {
                        Log.d(TAG, "findNeoPlayer: Scanning for NeoPlayer...");
                        DatagramSocket socket = new DatagramSocket();
                        socket.setBroadcast(true);
                        socket.setSoTimeout(1000);
                        byte[] message = ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(NeoPlayerToken).array();

                        for (Enumeration<NetworkInterface> niEnum = NetworkInterface.getNetworkInterfaces(); niEnum.hasMoreElements(); ) {
                            NetworkInterface ni = niEnum.nextElement();
                            if (ni.isLoopback())
                                continue;

                            for (InterfaceAddress interfaceAddress : ni.getInterfaceAddresses()) {
                                if (interfaceAddress.getBroadcast() == null)
                                    continue;

                                DatagramPacket packet = new DatagramPacket(message, message.length, interfaceAddress.getBroadcast(), NeoPlayerDefaultPort);
                                socket.send(packet);
                            }
                        }

                        DatagramPacket packet = new DatagramPacket(message, message.length);
                        while (true) {
                            socket.receive(packet);

                            if (packet.getData().length != 4)
                                continue;

                            if (ByteBuffer.wrap(packet.getData()).order(ByteOrder.LITTLE_ENDIAN).getInt() != NeoPlayerToken)
                                continue;

                            return packet.getAddress().getHostAddress() + ":" + NeoPlayerDefaultPort;
                        }
                    } catch (Exception ex) {
                        return null;
                    }
                }
            }.execute().get();
        } catch (Exception ex) {
            return null;
        }
    }
}
