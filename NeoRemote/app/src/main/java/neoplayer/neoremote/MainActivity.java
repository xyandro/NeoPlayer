package neoplayer.neoremote;

import android.app.Activity;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.databinding.DataBindingUtil;
import android.graphics.drawable.BitmapDrawable;
import android.os.AsyncTask;
import android.os.Build;
import android.os.Bundle;
import android.support.v4.app.NotificationCompat;
import android.support.v4.media.VolumeProviderCompat;
import android.support.v4.media.session.MediaSessionCompat;
import android.support.v4.media.session.PlaybackStateCompat;
import android.text.format.DateUtils;
import android.util.Log;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.RemoteViews;
import android.widget.SeekBar;

import java.io.IOException;
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
import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.concurrent.ArrayBlockingQueue;

import neoplayer.neoremote.databinding.MainActivityBinding;

public class MainActivity extends Activity {
    enum ViewType {Videos, Queue, History}

    private static final String TAG = MainActivity.class.getSimpleName();
    private static final String FakeProtocol = "ne://";
    private static final int NeoPlayerToken = 0xfeedbeef;
    private static final int NeoPlayerRestartToken = 0x0badf00d;
    private static final int NeoPlayerDefaultPort = 7399;
    private static final String addressFileName = "NeoPlayer.txt";

    private HashMap<Integer, VideoFile> videoFiles = new HashMap<>();
    private ViewType viewType = ViewType.Videos;
    private HashSet<Integer> starIDs = new HashSet<>();
    private HashMap<String, String> searchTags;
    private SortData sortData;
    private ArrayList<Integer> queue = new ArrayList<>();
    private ArrayList<Integer> history = new ArrayList<>();
    private MediaSessionCompat mediaSession;
    private VolumeProviderCompat volumeProvider;
    private boolean userTrackingSeekBar = false;
    private MainAdapter mainAdapter;
    private DownloadAdapter downloadAdapter;
    private static final LinkedHashMap<String, String> validSizes = new LinkedHashMap<>();
    private String currentSlidesQuery;
    private int currentSlidesSize;
    private ArrayBlockingQueue<byte[]> outputQueue = new ArrayBlockingQueue<>(100);
    private URI neoPlayerAddress;
    private Thread networkThread;
    private Socket socket;
    private NotificationManager notificationManager;
    private BroadcastReceiver broadcastReceiver;
    private RemoteViews remoteViews;
    private NotificationCompat.Builder notification;
    private boolean activityActive = false;
    private GetAddressDialog getAddressDialog;

    private MainActivityBinding binding;

    static {
        validSizes.put("Any size", "");
        validSizes.put("Large", "l");
        validSizes.put("Medium", "m");
        validSizes.put("Icon", "i");
        validSizes.put("400x300", "qsvga");
        validSizes.put("640x480", "vga");
        validSizes.put("800x600", "svga");
        validSizes.put("1024x768", "xga");
        validSizes.put("2 MP", "2mp");
        validSizes.put("4 MP", "4mp");
        validSizes.put("6 MP", "6mp");
        validSizes.put("8 MP", "8mp");
        validSizes.put("10 MP", "10mp");
        validSizes.put("12 MP", "12mp");
        validSizes.put("15 MP", "15mp");
        validSizes.put("20 MP", "20mp");
        validSizes.put("40 MP", "40mp");
        validSizes.put("70 MP", "70mp");
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        binding = DataBindingUtil.setContentView(this, R.layout.main_activity);

        setupMediaSession();
        setupControls();
        setupNotification();
        startNetworkThread();
    }

    private int seekBarToDisplayTime(int value) {
        if (value <= 0)
            return 2;
        if (value <= 6)
            return value * 5;
        if (value <= 8)
            return value * 15 - 60;
        if (value <= 12)
            return value * 30 - 180;
        return value * 60 - 540;
    }

    private int displayTimeToSeekBar(int value) {
        if (value <= 3)
            return 0;
        if (value <= 30)
            return (value + 2) / 5;
        if (value <= 60)
            return (value + 7) / 15 + 4;
        if (value <= 180)
            return (value + 14) / 30 + 6;
        return (value + 29) / 60 + 9;
    }

    private void setupControls() {
        mainAdapter = new MainAdapter(this, starIDs);
        downloadAdapter = new DownloadAdapter(this);

        new ScreenSlidePagerAdapter(binding.pager);
        binding.pager.setCurrentItem(1);

        binding.videoSearch.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                HashMap<String, String> tags = new HashMap<>();
                for (VideoFile videoFile : videoFiles.values())
                    for (String key : videoFile.tags.keySet())
                        tags.put(key, null);
                if (searchTags != null)
                    for (Map.Entry<String, String> searchTag : searchTags.entrySet())
                        tags.put(searchTag.getKey(), searchTag.getValue());
                FindDialog.createDialog(MainActivity.this, tags).show(getFragmentManager().beginTransaction(), EditTagsDialog.class.getName());
            }
        });
        binding.videoSearch.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                setSearchTags(null);
                return true;
            }
        });

        binding.videoSort.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                SortData useSortData = sortData;
                if (useSortData == null) {
                    HashSet<String> tags = new HashSet<>();
                    for (VideoFile videoFile : videoFiles.values())
                        for (String key : videoFile.tags.keySet())
                            tags.add(key);
                    useSortData = new SortData(tags);
                }
                SortDialog.createDialog(MainActivity.this, useSortData).show(getFragmentManager().beginTransaction(), EditTagsDialog.class.getName());
            }
        });

        binding.videoSort.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                setSortData(null);
                return true;
            }
        });

        binding.videos.setAdapter(mainAdapter);
        binding.videosEdit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                if (!starIDs.isEmpty()) {
                    ArrayList<VideoFile> editTagFiles = new ArrayList<>();
                    for (int videoFileID : starIDs)
                        editTagFiles.add(videoFiles.get(videoFileID));
                    EditTagsDialog.createDialog(MainActivity.this, EditTags.create(editTagFiles)).show(getFragmentManager().beginTransaction(), EditTagsDialog.class.getName());
                }
            }
        });
        binding.videosEdit.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                mainAdapter.clearStarIDs();
                return true;
            }
        });

        ArrayAdapter<String> adapter = new ArrayAdapter<>(this, R.layout.spinner_item, new ArrayList<>(validSizes.keySet()));
        binding.slidesSize.setAdapter(adapter);

        binding.slidesClear.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                binding.slidesQuery.setText(currentSlidesQuery);
                binding.slidesSize.setSelection(currentSlidesSize);
                binding.slidesQuery.clearFocus();
            }
        });

        binding.slidesSubmit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                binding.slidesQuery.clearFocus();
                String query = binding.slidesQuery.getText().toString();
                String size = validSizes.get(binding.slidesSize.getSelectedItem());
                outputQueue.add(new Message().add("SetSlidesQuery").add(query).add(size).toArray());
            }
        });

        binding.slidesDisplayTime.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int value, boolean fromUser) {
                int displayTime = seekBarToDisplayTime(value);
                value = displayTimeToSeekBar(displayTime);
                seekBar.setProgress(value);
                binding.slidesDisplayTimeText.setText(DateUtils.formatElapsedTime(displayTime));
                if (fromUser)
                    outputQueue.add(new Message().add("SetSlideDisplayTime").add(displayTime).toArray());
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
            }
        });

        binding.slidesBack.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("CycleSlide").add(false).toArray());
            }
        });

        binding.slidesPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("ToggleSlidesPlaying").toArray());
            }
        });

        binding.slidesForward.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("CycleSlide").add(true).toArray());
            }
        });

        binding.videoList.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                viewType = ViewType.Videos;
                updateVideoList();
            }
        });

        binding.videoQueue.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                viewType = ViewType.Queue;
                updateVideoList();
            }
        });

        binding.videoHistory.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                viewType = ViewType.History;
                updateVideoList();
            }
        });

        binding.downloadVideosList.setAdapter(downloadAdapter);
        binding.downloadSubmit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                String url = binding.downloadUrl.getText().toString();
                if (!url.equals("")) {
                    binding.downloadUrl.setText("");
                    binding.downloadUrl.clearFocus();
                    outputQueue.add(new Message().add("DownloadURL").add(url).toArray());
                }
            }
        });

        binding.navbarSeekBar.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int value, boolean fromUser) {
                binding.navbarCurrentTime.setText(DateUtils.formatElapsedTime(value));
            }

            @Override
            public void onStartTrackingTouch(SeekBar seekBar) {
                userTrackingSeekBar = true;
            }

            @Override
            public void onStopTrackingTouch(SeekBar seekBar) {
                outputQueue.add(new Message().add("SetPosition").add(seekBar.getProgress()).add(false).toArray());
                userTrackingSeekBar = false;
            }
        });

        binding.navbarBack30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(-30).add(true).toArray());
            }
        });

        binding.navbarBack5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(-5).add(true).toArray());
            }
        });

        binding.navbarPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("ToggleMediaPlaying").add(false).toArray());
            }
        });

        binding.navbarPlay.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                outputQueue.add(new Message().add("ToggleMediaPlaying").add(true).toArray());
                return true;
            }
        });

        binding.navbarForward5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(5).add(true).toArray());
            }
        });

        binding.navbarForward30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(30).add(true).toArray());
            }
        });

        binding.navbarForward.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("MediaForward").toArray());
            }
        });
    }

    private void sendRestart() {
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

    private void setupNotification() {
        notificationManager = (NotificationManager) getSystemService(Context.NOTIFICATION_SERVICE);
        String NOTIFICATION_CHANNEL_ID = "NeoPlayer";

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel notificationChannel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "NeoPlayer", NotificationManager.IMPORTANCE_NONE);
            notificationChannel.setDescription("NeoPlayer");
            notificationManager.createNotificationChannel(notificationChannel);
        }

        IntentFilter intentFilter = new IntentFilter();
        intentFilter.addAction("com.neoremote.android.PlayPause");
        intentFilter.addAction("com.neoremote.android.Forward");
        broadcastReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                switch (intent.getAction()) {
                    case "com.neoremote.android.PlayPause":
                        outputQueue.add(new Message().add("ToggleMediaPlaying").add(false).toArray());
                        break;
                    case "com.neoremote.android.Forward":
                        outputQueue.add(new Message().add("MediaForward").toArray());
                        break;
                }
            }
        };
        registerReceiver(broadcastReceiver, intentFilter);

        remoteViews = new RemoteViews(getPackageName(), R.layout.notification);
        remoteViews.setOnClickPendingIntent(R.id.notification_play_pause, PendingIntent.getBroadcast(this, 0, new Intent("com.neoremote.android.PlayPause"), PendingIntent.FLAG_UPDATE_CURRENT));
        remoteViews.setOnClickPendingIntent(R.id.notification_forward, PendingIntent.getBroadcast(this, 0, new Intent("com.neoremote.android.Forward"), PendingIntent.FLAG_UPDATE_CURRENT));

        Intent notificationIntent = new Intent(getApplicationContext(), MainActivity.class);
        notificationIntent.setFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        PendingIntent intent = PendingIntent.getActivity(getApplicationContext(), 0, notificationIntent, 0);

        notification = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID);
        notification.setSmallIcon(R.mipmap.notification);
        notification.setContent(remoteViews);
        notification.setContentIntent(intent);

        setNotification();
    }

    private void setNotification() {
        remoteViews.setTextViewText(R.id.notification_text, binding.navbarTitle.getText());
        remoteViews.setImageViewBitmap(R.id.notification_play_pause, ((BitmapDrawable) binding.navbarPlay.getDrawable()).getBitmap());
        notificationManager.notify(0, notification.build());
    }

    private void clearNotification() {
        notificationManager.cancel(0);
        unregisterReceiver(broadcastReceiver);
    }

    private void setupMediaSession() {
        mediaSession = new MediaSessionCompat(this, "NeoRemoteMediaSession");
        mediaSession.setPlaybackState(new PlaybackStateCompat.Builder()
                .setState(PlaybackStateCompat.STATE_PLAYING, PlaybackStateCompat.PLAYBACK_POSITION_UNKNOWN, 1.0f)
                .build());
        mediaSession.setCallback(new MediaSessionCompat.Callback() {
            @Override
            public boolean onMediaButtonEvent(final Intent mediaButtonEvent) {
                Log.d(TAG, "onMediaButtonEvent() called with: " + "mediaButtonEvent = [" + mediaButtonEvent + "]");
                return super.onMediaButtonEvent(mediaButtonEvent);
            }
        });

        volumeProvider = new VolumeProviderCompat(VolumeProviderCompat.VOLUME_CONTROL_ABSOLUTE, 25, 13) {
            @Override
            public void onSetVolumeTo(int volume) {
                outputQueue.add(new Message().add("SetVolume").add(volume * 4).add(false).toArray());
            }

            @Override
            public void onAdjustVolume(int delta) {
                outputQueue.add(new Message().add("SetVolume").add(delta * 4).add(true).toArray());
            }
        };

        mediaSession.setPlaybackToRemote(volumeProvider);
        mediaSession.setActive(true);
    }

    private void clearMediaSession() {
        mediaSession.release();
    }

    @Override
    protected void onResume() {
        super.onResume();
        activityActive = true;
    }

    @Override
    protected void onPause() {
        super.onPause();
        activityActive = false;
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();

        stopNetworkThread();
        clearNotification();
        clearMediaSession();
        finish();
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

    private void startNetworkThread() {
        networkThread = new Thread(new Runnable() {
            @Override
            public void run() {
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

                        runOnUiThread(new Runnable() {
                            @Override
                            public void run() {
                                clearGetAddressDialog();
                            }
                        });

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
                                runOnUiThread(new Runnable() {
                                    @Override
                                    public void run() {
                                        handleMessage(message);
                                    }
                                });
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

                    if (System.nanoTime() - startTime >= 1000000000)
                        runOnUiThread(new Runnable() {
                            @Override
                            public void run() {
                                showGetAddressDialog();
                            }
                        });
                }
                Log.d(TAG, "startNetworkThread: Stopped");
            }
        });
        networkThread.start();
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

    private void stopNetworkThread() {
        Thread networkThread = this.networkThread;
        this.networkThread = null;
        Socket socket = this.socket;
        if (socket != null)
            try {
                socket.close();
            } catch (IOException e) {
            }

        try {
            networkThread.join();
        } catch (InterruptedException e) {
        }
    }

    private void showGetAddressDialog() {
        if ((!activityActive) || (getAddressDialog != null))
            return;
        getAddressDialog = new GetAddressDialog();
        getAddressDialog.mainActivity = MainActivity.this;
        getAddressDialog.address = neoPlayerAddress == null ? "" : neoPlayerAddress.toString().substring(FakeProtocol.length());
        getAddressDialog.show(getFragmentManager(), "NoticeDialogFragment");
    }

    private void clearGetAddressDialog() {
        if (getAddressDialog == null)
            return;
        getAddressDialog.dismiss();
        getAddressDialog = null;
    }

    public void setAddress(final String address) {
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

    public void queueVideo(int videoFileID, boolean top) {
        outputQueue.add(new Message().add("QueueVideo").add(videoFileID).add(top).toArray());
    }

    public void editTags(EditTags editTags) {
        outputQueue.add(new Message().add("EditTags").add(editTags).toArray());
    }

    public void setSearchTags(HashMap<String, String> searchTags) {
        this.searchTags = searchTags;
        updateVideoList();
    }

    public void setSortData(SortData sortData) {
        this.sortData = sortData;
        updateVideoList();
    }

    private void updateVideoList() {
        switch (viewType) {
            case Queue:
                mainAdapter.setShowIDs(queue);
                break;
            case History:
                mainAdapter.setShowIDs(history);
                break;
            default:
                ArrayList<Integer> showIDs = new ArrayList<>();
                for (VideoFile videoFile : videoFiles.values())
                    if (matchSearch(videoFile))
                        showIDs.add(videoFile.videoFileID);
                sortIDs(showIDs);
                mainAdapter.setShowIDs(showIDs);
                break;
        }
    }

    private boolean matchSearch(VideoFile videoFile) {
        if (searchTags == null)
            return true;

        for (Map.Entry<String, String> searchTag : searchTags.entrySet()) {
            String value = videoFile.tags.get(searchTag.getKey());
            if (value == null)
                return false;
            if (!value.toLowerCase().contains(searchTag.getValue()))
                return false;
        }

        return true;
    }

    private void sortIDs(ArrayList<Integer> showIDs) {
        Collections.sort(showIDs, new Comparator<Integer>() {
            @Override
            public int compare(Integer id1, Integer id2) {
                VideoFile videoFile1 = videoFiles.get(id1);
                VideoFile videoFile2 = videoFiles.get(id2);
                if (sortData != null) {
                    for (int ctr = 1; ; ++ctr) {
                        SortData.SortItem sortItem = sortData.getPriorityOrder(ctr);
                        if (sortItem == null)
                            break;

                        if (sortItem.direction == SortData.SortDirection.None)
                            continue;
                        int compare = Helpers.stringCompare(videoFile1.tags.get(sortItem.tag), videoFile2.tags.get(sortItem.tag), false, true);
                        if (compare == 0)
                            continue;
                        if (sortItem.direction == SortData.SortDirection.Descending)
                            compare *= -1;
                        return compare;
                    }
                }
                return Helpers.stringCompare(videoFile1.getTitle(), videoFile2.getTitle(), false, true);
            }
        });
    }

    private void handleMessage(Message message) {
        int count = message.getInt();
        while (count > 0) {
            --count;
            String field = message.getString();
            switch (field) {
                case "History":
                    history = message.getInts();
                    updateVideoList();
                    break;
                case "Queue":
                    queue = message.getInts();
                    mainAdapter.setCheckIDs(new HashSet<>(queue));
                    updateVideoList();
                    break;
                case "VideoFiles":
                    videoFiles = new HashMap<>();
                    for (VideoFile videoFile : message.getVideoFiles())
                        videoFiles.put(videoFile.videoFileID, videoFile);
                    mainAdapter.setVideoFiles(videoFiles);
                    updateVideoList();
                    break;
                case "Downloads":
                    downloadAdapter.setDownloadData(message.getDownloadDatas());
                    binding.downloadVideosList.smoothScrollToPosition(0);
                    break;
                case "MediaVolume":
                    volumeProvider.setCurrentVolume(message.getInt() / 4);
                    break;
                case "MediaTitle":
                    String title = message.getString();
                    binding.navbarTitle.setText(title);
                    setNotification();
                    break;
                case "MediaPosition":
                    int position = message.getInt();
                    if (!userTrackingSeekBar)
                        binding.navbarSeekBar.setProgress(position);
                    break;
                case "MediaMaxPosition":
                    int maxPosition = message.getInt();
                    binding.navbarSeekBar.setMax(maxPosition);
                    binding.navbarMaxTime.setText(DateUtils.formatElapsedTime(maxPosition));
                    break;
                case "MediaPlaying":
                    int value = message.getInt();
                    int drawable = value == 0 ? R.drawable.play : value == 1 ? R.drawable.pause : R.drawable.allplay;
                    binding.navbarPlay.setImageResource(drawable);
                    setNotification();
                    break;
                case "SlidesQuery":
                    currentSlidesQuery = message.getString();
                    binding.slidesQuery.setText(currentSlidesQuery);
                    break;
                case "SlidesSize":
                    int index = 0;
                    String slidesSizeStr = message.getString();
                    for (String entry : validSizes.keySet()) {
                        if (validSizes.get(entry).equals(slidesSizeStr)) {
                            currentSlidesSize = index;
                            binding.slidesSize.setSelection(index);
                        }
                        ++index;
                    }
                    break;
                case "SlideDisplayTime":
                    binding.slidesDisplayTime.setProgress(displayTimeToSeekBar(message.getInt()));
                    break;
                case "SlidesPlaying":
                    binding.slidesPlay.setImageResource(message.getBool() ? R.drawable.pause : R.drawable.play);
                    break;
            }
        }
    }
}
