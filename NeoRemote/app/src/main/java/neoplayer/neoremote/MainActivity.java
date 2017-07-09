package neoplayer.neoremote;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.AsyncTask;
import android.os.Bundle;
import android.support.v4.media.VolumeProviderCompat;
import android.support.v4.media.session.MediaSessionCompat;
import android.support.v4.media.session.PlaybackStateCompat;
import android.support.v4.view.ViewPager;
import android.text.Editable;
import android.text.TextWatcher;
import android.text.format.DateUtils;
import android.util.Log;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.ImageButton;
import android.widget.ListView;
import android.widget.SeekBar;
import android.widget.Spinner;
import android.widget.TextView;

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
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.ArrayList;
import java.util.Enumeration;
import java.util.LinkedHashMap;
import java.util.concurrent.ArrayBlockingQueue;

public class MainActivity extends Activity {
    private static final String TAG = MainActivity.class.getSimpleName();

    private final ArrayList<MediaData> queueVideos = new ArrayList<>();
    private final ArrayList<MediaData> coolVideos = new ArrayList<>();
    private final ArrayList<MediaData> youTubeVideos = new ArrayList<>();
    private final ArrayList<MediaData> moviesVideos = new ArrayList<>();
    private MediaSessionCompat mediaSession;
    private VolumeProviderCompat volumeProvider;
    private boolean userTrackingSeekBar = false;
    private final MediaListAdapter queueAdapter;
    private final MediaListAdapter coolAdapter;
    private final MediaListAdapter youTubeAdapter;
    private final MediaListAdapter moviesAdapter;
    private static final LinkedHashMap<String, String> validSizes = new LinkedHashMap<>();
    private String currentSlidesQuery;
    private int currentSlidesSize;
    private static final int NeoPlayerToken = 0xfeedbeef;
    private static final int NeoPlayerRestartToken = 0x0badf00d;
    private static final int NeoPlayerPort = 7398;
    private static final int NeoPlayerRestartPort = 7397;
    private ArrayBlockingQueue<byte[]> outputQueue = new ArrayBlockingQueue<>(100);
    private InetAddress neoPlayerAddress = null;
    private static final String addressFileName = "NeoPlayer.txt";
    private Thread readerThread;
    private Socket socket = null;

    private ViewPager pager;
    private NEEditText queueSearchText;
    private ImageButton queueClearSearch;
    private ListView queueVideosList;
    private NEEditText coolSearchText;
    private ImageButton coolClearSearch;
    private ListView coolVideosList;
    private NEEditText moviesSearchText;
    private ImageButton moviesClearSearch;
    private ListView moviesVideosList;
    private NEEditText slidesQuery;
    private Spinner slidesSize;
    private ImageButton slidesClear;
    private ImageButton slidesSubmit;
    private SeekBar slidesDisplayTime;
    private TextView slidesDisplayTimeText;
    private ImageButton slidesBack;
    private ImageButton slidesPlay;
    private ImageButton slidesForward;
    private NEEditText youtubeSearchText;
    private ImageButton youtubeSubmit;
    private ListView youtubeVideosList;
    private TextView navbarTitle;
    private TextView navbarCurrentTime;
    private SeekBar navbarSeekBar;
    private TextView navbarMaxTime;
    private ImageButton navbarBack30;
    private ImageButton navbarBack5;
    private ImageButton navbarPlay;
    private ImageButton navbarForward5;
    private ImageButton navbarForward30;
    private ImageButton navbarForward;

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

    public MainActivity() {
        queueAdapter = new MediaListAdapter(this, queueVideos, queueVideos);
        coolAdapter = new MediaListAdapter(this, coolVideos, queueVideos);
        youTubeAdapter = new MediaListAdapter(this, youTubeVideos, queueVideos);
        moviesAdapter = new MediaListAdapter(this, moviesVideos, queueVideos);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        prepareMediaSession();
        setupControls();

        readerThread = new Thread(new Runnable() {
            @Override
            public void run() {
                runReaderThread();
            }
        });
        readerThread.start();
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
        pager = findViewById(R.id.pager);
        queueSearchText = findViewById(R.id.queue_search_text);
        queueClearSearch = findViewById(R.id.queue_clear_search);
        queueVideosList = findViewById(R.id.queue_videos_list);
        coolSearchText = findViewById(R.id.cool_search_text);
        coolClearSearch = findViewById(R.id.cool_clear_search);
        coolVideosList = findViewById(R.id.cool_videos_list);
        moviesSearchText = findViewById(R.id.movies_search_text);
        moviesClearSearch = findViewById(R.id.movies_clear_search);
        moviesVideosList = findViewById(R.id.movies_videos_list);
        slidesQuery = findViewById(R.id.slides_query);
        slidesSize = findViewById(R.id.slides_size);
        slidesClear = findViewById(R.id.slides_clear);
        slidesSubmit = findViewById(R.id.slides_submit);
        slidesDisplayTime = findViewById(R.id.slides_display_time);
        slidesDisplayTimeText = findViewById(R.id.slides_display_time_text);
        slidesBack = findViewById(R.id.slides_back);
        slidesPlay = findViewById(R.id.slides_play);
        slidesForward = findViewById(R.id.slides_forward);
        youtubeSearchText = findViewById(R.id.youtube_search_text);
        youtubeSubmit = findViewById(R.id.youtube_submit);
        youtubeVideosList = findViewById(R.id.youtube_videos_list);
        navbarTitle = findViewById(R.id.navbar_title);
        navbarCurrentTime = findViewById(R.id.navbar_current_time);
        navbarSeekBar = findViewById(R.id.navbar_seek_bar);
        navbarMaxTime = findViewById(R.id.navbar_max_time);
        navbarBack30 = findViewById(R.id.navbar_back30);
        navbarBack5 = findViewById(R.id.navbar_back5);
        navbarPlay = findViewById(R.id.navbar_play);
        navbarForward5 = findViewById(R.id.navbar_forward5);
        navbarForward30 = findViewById(R.id.navbar_forward30);
        navbarForward = findViewById(R.id.navbar_forward);

        new ScreenSlidePagerAdapter(pager);
        pager.setCurrentItem(1);

        queueSearchText.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                queueAdapter.setFilter(charSequence.toString());
            }

            @Override
            public void afterTextChanged(Editable editable) {
            }
        });

        queueVideosList.setAdapter(queueAdapter);
        queueClearSearch.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                queueSearchText.clearFocus();
                queueSearchText.setText("");
            }
        });

        queueClearSearch.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                new AlertDialog.Builder(MainActivity.this)
                        .setIcon(android.R.drawable.ic_dialog_alert)
                        .setTitle("Restart NeoPlayer?")
                        .setMessage("Are you sure you want to restart NeoPlayer?")
                        .setNegativeButton("Yes", new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                sendRestart();
                            }

                        })
                        .setPositiveButton("No", null)
                        .show();

                return false;
            }
        });

        coolSearchText.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                coolAdapter.setFilter(charSequence.toString());
            }

            @Override
            public void afterTextChanged(Editable editable) {
            }
        });

        coolVideosList.setAdapter(coolAdapter);
        coolClearSearch.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                coolSearchText.clearFocus();
                coolSearchText.setText("");
            }
        });

        moviesSearchText.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                moviesAdapter.setFilter(charSequence.toString());
            }

            @Override
            public void afterTextChanged(Editable editable) {
            }
        });

        moviesVideosList.setAdapter(moviesAdapter);
        moviesClearSearch.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                moviesSearchText.clearFocus();
                moviesSearchText.setText("");
            }
        });

        ArrayAdapter<String> adapter = new ArrayAdapter<>(this, R.layout.spinner_item, new ArrayList<>(validSizes.keySet()));
        slidesSize.setAdapter(adapter);

        slidesClear.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                slidesQuery.setText(currentSlidesQuery);
                slidesSize.setSelection(currentSlidesSize);
                slidesQuery.clearFocus();
            }
        });

        slidesSubmit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                slidesQuery.clearFocus();
                String query = slidesQuery.getText().toString();
                String size = validSizes.get(slidesSize.getSelectedItem());
                outputQueue.add(new Message().add("SetSlidesQuery").add(query).add(size).toArray());
            }
        });

        slidesDisplayTime.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int value, boolean fromUser) {
                int displayTime = seekBarToDisplayTime(value);
                value = displayTimeToSeekBar(displayTime);
                seekBar.setProgress(value);
                slidesDisplayTimeText.setText(DateUtils.formatElapsedTime(displayTime));
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

        slidesBack.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("CycleSlide").add(false).toArray());
            }
        });

        slidesPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("ToggleSlidesPlaying").toArray());
            }
        });

        slidesForward.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("CycleSlide").add(true).toArray());
            }
        });

        youtubeVideosList.setAdapter(youTubeAdapter);
        youtubeSubmit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                youtubeSearchText.clearFocus();
                outputQueue.add(new Message().add("SearchYouTube").add(youtubeSearchText.getText().toString()).toArray());
            }
        });

        navbarSeekBar.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar seekBar, int value, boolean fromUser) {
                navbarCurrentTime.setText(DateUtils.formatElapsedTime(value));
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

        navbarBack30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(-30).add(true).toArray());
            }
        });

        navbarBack5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(-5).add(true).toArray());
            }
        });

        navbarPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("ToggleMediaPlaying").toArray());
            }
        });

        navbarForward5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(5).add(true).toArray());
            }
        });

        navbarForward30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                outputQueue.add(new Message().add("SetPosition").add(30).add(true).toArray());
            }
        });

        navbarForward.setOnClickListener(new View.OnClickListener() {
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
                        new Socket(neoPlayerAddress, NeoPlayerRestartPort).getOutputStream().write(ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(NeoPlayerRestartToken).array());
                } catch (Exception e) {
                }
                return null;
            }
        }.execute();
    }

    private void prepareMediaSession() {
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

    private GetAddressDialog getAddressDialog;

    boolean activityActive = false;

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
        Thread readerThread = this.readerThread;
        this.readerThread = null;
        Socket socket = this.socket;
        if (socket != null)
            try {
                socket.close();
            } catch (IOException e) {
            }

        try {
            readerThread.join();
        } catch (InterruptedException e) {
        }
        mediaSession.release();
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
                neoPlayerAddress = InetAddress.getByName(address);
            } finally {
                in.close();
            }
        } catch (Exception ex) {
        }
    }

    private void runReaderThread() {
        getNeoPlayerAddress();

        boolean first = true;
        Log.d(TAG, "runReaderThread: Started");
        while (readerThread != null) try {
            if (!first)
                Thread.sleep(1000);
            first = false;

            socket = new Socket();
            socket.connect(new InetSocketAddress(neoPlayerAddress, NeoPlayerPort), 1000);

            try {
                Log.d(TAG, "runReaderThread: Connected");

                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        if (getAddressDialog != null) {
                            getAddressDialog.dismiss();
                            getAddressDialog = null;
                        }
                    }
                });

                outputQueue.clear();

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
            Log.d(TAG, "runReaderThread: Error: " + ex.getMessage());

            runOnUiThread(new Runnable() {
                @Override
                public void run() {
                    if (activityActive)
                        if (getAddressDialog == null) {
                            getAddressDialog = new GetAddressDialog(MainActivity.this, neoPlayerAddress == null ? "" : neoPlayerAddress.getHostAddress());
                            getAddressDialog.show(getFragmentManager(), "NoticeDialogFragment");
                        }
                }
            });
        }
        Log.d(TAG, "runReaderThread: Stopped");
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

    public void setAddress(final String address) {
        try {
            neoPlayerAddress = new AsyncTask<Void, Void, InetAddress>() {
                @Override
                protected InetAddress doInBackground(Void... voids) {
                    try {
                        return InetAddress.getByName(address);
                    } catch (Exception ex) {
                        return null;
                    }
                }
            }.execute().get();
            if (neoPlayerAddress == null)
                return;

            byte[] buffer = address.getBytes("UTF-8");
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

                                DatagramPacket packet = new DatagramPacket(message, message.length, interfaceAddress.getBroadcast(), NeoPlayerPort);
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

                            return packet.getAddress().getHostAddress();
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

    public void queueVideo(MediaData mediaData) {
        outputQueue.add(new Message().add("QueueVideo").add(mediaData).toArray());
    }

    private void handleMessage(Message message) {
        int count = message.getInt();
        while (count > 0) {
            --count;
            String field = message.getString();
            switch (field) {
                case "Queue":
                    queueVideos.clear();
                    for (MediaData mediaData : message.getMediaDatas())
                        queueVideos.add(mediaData);
                    queueAdapter.notifyDataSetChanged();
                    coolAdapter.notifyDataSetChanged();
                    youTubeAdapter.notifyDataSetChanged();
                    moviesAdapter.notifyDataSetChanged();
                    break;
                case "Cool":
                    coolVideos.clear();
                    for (MediaData mediaData : message.getMediaDatas())
                        coolVideos.add(mediaData);
                    coolAdapter.notifyDataSetChanged();
                    break;
                case "YouTube":
                    youTubeVideos.clear();
                    for (MediaData mediaData : message.getMediaDatas())
                        youTubeVideos.add(mediaData);
                    youTubeAdapter.notifyDataSetChanged();
                    youtubeVideosList.smoothScrollToPosition(0);
                    break;
                case "Movies":
                    moviesVideos.clear();
                    for (MediaData mediaData : message.getMediaDatas())
                        moviesVideos.add(mediaData);
                    moviesAdapter.notifyDataSetChanged();
                    break;
                case "MediaVolume":
                    volumeProvider.setCurrentVolume(message.getInt() / 4);
                    break;
                case "MediaTitle":
                    navbarTitle.setText(message.getString());
                    break;
                case "MediaPosition":
                    int position = message.getInt();
                    if (!userTrackingSeekBar)
                        navbarSeekBar.setProgress(position);
                    break;
                case "MediaMaxPosition":
                    int maxPosition = message.getInt();
                    navbarSeekBar.setMax(maxPosition);
                    navbarMaxTime.setText(DateUtils.formatElapsedTime(maxPosition));
                    break;
                case "MediaPlaying":
                    navbarPlay.setImageResource(message.getBool() ? R.drawable.pause : R.drawable.play);
                    break;
                case "SlidesQuery":
                    currentSlidesQuery = message.getString();
                    slidesQuery.setText(currentSlidesQuery);
                    break;
                case "SlidesSize":
                    int index = 0;
                    String slidesSizeStr = message.getString();
                    for (String entry : validSizes.keySet()) {
                        if (validSizes.get(entry).equals(slidesSizeStr)) {
                            currentSlidesSize = index;
                            slidesSize.setSelection(index);
                        }
                        ++index;
                    }
                    break;
                case "SlideDisplayTime":
                    slidesDisplayTime.setProgress(displayTimeToSeekBar(message.getInt()));
                    break;
                case "SlidesPlaying":
                    slidesPlay.setImageResource(message.getBool() ? R.drawable.pause : R.drawable.play);
                    break;
            }
        }
    }
}
