package neoplayer.neoremote;

import android.app.Activity;
import android.app.AlertDialog;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.ServiceConnection;
import android.databinding.DataBindingUtil;
import android.graphics.drawable.BitmapDrawable;
import android.os.Build;
import android.os.Bundle;
import android.os.IBinder;
import android.support.v4.app.NotificationCompat;
import android.support.v4.content.LocalBroadcastManager;
import android.support.v4.media.VolumeProviderCompat;
import android.support.v4.media.session.MediaSessionCompat;
import android.support.v4.media.session.PlaybackStateCompat;
import android.text.format.DateUtils;
import android.util.Log;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.RemoteViews;
import android.widget.SeekBar;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;

import neoplayer.neoremote.databinding.MainActivityBinding;

public class MainActivity extends Activity {
    enum ViewType {Videos, Queue, History}

    private static final String TAG = MainActivity.class.getSimpleName();

    private HashMap<Integer, VideoFile> videoFiles = new HashMap<>();
    private ViewType viewType = ViewType.Videos;
    private LinkedHashSet<Integer> starIDs = new LinkedHashSet<>();
    private ArrayList<FindData> findDataList;
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
    private NotificationManager notificationManager;
    private BroadcastReceiver notificationBroadcastReceiver;
    private RemoteViews remoteViews;
    private NotificationCompat.Builder notification;
    private boolean activityActive = false;
    private GetAddressDialog getAddressDialog;
    private NetworkService networkService;
    private BroadcastReceiver serviceBroadcastReceiver;
    private ServiceConnection serviceConnection;

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
        setupService();
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
                QuickFindDialog quickFindDialog = new QuickFindDialog();
                quickFindDialog.mainActivity = MainActivity.this;
                if (findDataList != null)
                    for (FindData findData : findDataList)
                        if (findData.tag.equals("Title"))
                            quickFindDialog.findText = findData.value1;
                quickFindDialog.show(getFragmentManager(), "NoticeDialogFragment");
            }
        });
        binding.videoSearch.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                HashMap<String, FindData> tags = new HashMap<>();
                if (findDataList != null)
                    for (FindData findData : findDataList)
                        tags.put(findData.tag, findData.copy());
                for (VideoFile videoFile : videoFiles.values())
                    for (String tag : videoFile.tags.keySet())
                        if (!tags.containsKey(tag))
                            tags.put(tag, new FindData(tag));
                FindDialog.createDialog(MainActivity.this, new ArrayList<>(tags.values())).show(getFragmentManager().beginTransaction(), EditTagsDialog.class.getName());
                return true;
            }
        });

        binding.videoSort.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                if (sortData != null)
                    setSortData(null);
                else {
                    HashSet<String> tags = new HashSet<>();
                    for (VideoFile videoFile : videoFiles.values())
                        for (String key : videoFile.tags.keySet())
                            tags.add(key);
                    SortData useSortData = new SortData(tags);
                    for (int ctr = 0; ctr < useSortData.size(); ++ctr) {
                        SortData.SortItem sortItem = useSortData.getAlphaOrder(ctr);
                        if (sortItem.tag.equals("DownloadDate"))
                            useSortData.setSortDirection(sortItem, SortData.SortDirection.Descending);
                    }
                    setSortData(useSortData);
                }
            }
        });

        binding.videoSort.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                SortData useSortData = sortData;
                if (useSortData == null) {
                    HashSet<String> tags = new HashSet<>();
                    for (VideoFile videoFile : videoFiles.values())
                        for (String key : videoFile.tags.keySet())
                            tags.add(key);
                    useSortData = new SortData(tags);
                }
                SortDialog.createDialog(MainActivity.this, useSortData).show(getFragmentManager().beginTransaction(), EditTagsDialog.class.getName());
                return true;
            }
        });

        binding.videos.setAdapter(mainAdapter);
        binding.videoEdit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                ArrayList<VideoFile> editTagFiles = new ArrayList<>();
                for (int videoFileID : starIDs)
                    if (videoFiles.containsKey(videoFileID))
                        editTagFiles.add(videoFiles.get(videoFileID));
                if (!editTagFiles.isEmpty()) {
                    EditTagsDialog.createDialog(MainActivity.this, EditTags.create(editTagFiles)).show(getFragmentManager().beginTransaction(), EditTagsDialog.class.getName());
                }
            }
        });
        binding.videoEdit.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                if (starIDs.isEmpty())
                    mainAdapter.starShowIDs();
                else
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

        binding.slidesClear.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                new AlertDialog.Builder(MainActivity.this)
                        .setIcon(android.R.drawable.ic_dialog_alert)
                        .setTitle("Restart NeoPlayer?")
                        .setMessage("Are you sure you want to restart NeoPlayer?")
                        .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                networkService.sendRestart();
                            }

                        })
                        .setNegativeButton("No", null)
                        .show();
                return true;
            }
        });

        binding.slidesSubmit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                binding.slidesQuery.clearFocus();
                String query = binding.slidesQuery.getText().toString();
                String size = validSizes.get(binding.slidesSize.getSelectedItem());
                sendMessage(new Message().add("SetSlidesQuery").add(query).add(size).toArray());
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
                    sendMessage(new Message().add("SetSlideDisplayTime").add(displayTime).toArray());
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
                sendMessage(new Message().add("CycleSlide").add(false).toArray());
            }
        });

        binding.slidesPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("ToggleSlidesPlaying").toArray());
            }
        });

        binding.slidesForward.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("CycleSlide").add(true).toArray());
            }
        });

        binding.videoList.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                viewType = ViewType.Videos;
                updateVideoList();
            }
        });

        binding.videoList.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                viewType = ViewType.Videos;
                setFindDataList(null);
                setSortData(null);
                return true;
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
                    sendMessage(new Message().add("DownloadURL").add(url).toArray());
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
                sendMessage(new Message().add("SetPosition").add(seekBar.getProgress()).add(false).toArray());
                userTrackingSeekBar = false;
            }
        });

        binding.navbarBack30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("SetPosition").add(-30).add(true).toArray());
            }
        });

        binding.navbarBack5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("SetPosition").add(-5).add(true).toArray());
            }
        });

        binding.navbarPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("ToggleMediaPlaying").add(false).toArray());
            }
        });

        binding.navbarPlay.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                sendMessage(new Message().add("ToggleMediaPlaying").add(true).toArray());
                return true;
            }
        });

        binding.navbarForward5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("SetPosition").add(5).add(true).toArray());
            }
        });

        binding.navbarForward30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("SetPosition").add(30).add(true).toArray());
            }
        });

        binding.navbarForward.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sendMessage(new Message().add("MediaForward").toArray());
            }
        });
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
        notificationBroadcastReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                switch (intent.getAction()) {
                    case "com.neoremote.android.PlayPause":
                        sendMessage(new Message().add("ToggleMediaPlaying").add(false).toArray());
                        break;
                    case "com.neoremote.android.Forward":
                        sendMessage(new Message().add("MediaForward").toArray());
                        break;
                }
            }
        };
        registerReceiver(notificationBroadcastReceiver, intentFilter);

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
        unregisterReceiver(notificationBroadcastReceiver);
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
                sendMessage(new Message().add("SetVolume").add(volume * 4).add(false).toArray());
            }

            @Override
            public void onAdjustVolume(int delta) {
                sendMessage(new Message().add("SetVolume").add(delta * 4).add(true).toArray());
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

        clearService();
        clearNotification();
        clearMediaSession();
        finish();
    }

    private void setupService() {
        serviceBroadcastReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                switch (intent.getStringExtra("action")) {
                    case "showGetAddressDialog":
                        showGetAddressDialog(intent.getStringExtra("address"));
                        break;
                    case "clearGetAddressDialog":
                        clearGetAddressDialog();
                        break;
                    case "handleMessage":
                        handleMessage(intent.getByteArrayExtra("message"));
                        break;
                }
            }
        };
        LocalBroadcastManager.getInstance(this).registerReceiver(serviceBroadcastReceiver, new IntentFilter(NetworkService.ServiceIntent));

        serviceConnection = new ServiceConnection() {
            @Override
            public void onServiceConnected(ComponentName className, IBinder service) {
                networkService = ((NetworkService.NetworkServiceBinder) service).getService();
            }

            @Override
            public void onServiceDisconnected(ComponentName arg0) {
                finish();
            }
        };

        bindService(new Intent(this, NetworkService.class), serviceConnection, Context.BIND_AUTO_CREATE);
    }

    private void clearService() {
        LocalBroadcastManager.getInstance(this).unregisterReceiver(serviceBroadcastReceiver);
        unbindService(serviceConnection);
    }

    private void showGetAddressDialog(String neoPlayerAddress) {
        if ((!activityActive) || (getAddressDialog != null))
            return;
        getAddressDialog = new GetAddressDialog();
        getAddressDialog.mainActivity = MainActivity.this;
        getAddressDialog.networkService = networkService;
        getAddressDialog.address = neoPlayerAddress;
        getAddressDialog.show(getFragmentManager(), "NoticeDialogFragment");
    }

    private void clearGetAddressDialog() {
        if (getAddressDialog == null)
            return;
        getAddressDialog.dismiss();
        getAddressDialog = null;
    }

    public void queueVideo(int videoFileID, boolean top) {
        ArrayList videoFileIDs = new ArrayList();
        videoFileIDs.add(videoFileID);
        queueVideos(videoFileIDs, top);
    }

    public void queueVideos(ArrayList<Integer> videoFileIDs, boolean top) {
        sendMessage(new Message().add("QueueVideos").add(videoFileIDs).add(top).toArray());
    }

    public void deleteVideos(ArrayList<Integer> videoFileIDs) {
        sendMessage(new Message().add("DeleteVideos").add(videoFileIDs).toArray());
    }

    public void editTags(EditTags editTags) {
        sendMessage(new Message().add("EditTags").add(editTags).toArray());
    }

    public void setFindDataList(ArrayList<FindData> findDataList) {
        this.findDataList = findDataList;
        updateVideoList();
    }

    public void setSortData(SortData sortData) {
        this.sortData = sortData;
        updateVideoList();
    }

    private void updateVideoList() {
        switch (viewType) {
            case Queue:
                binding.title.setText("Queue");
                mainAdapter.setShowIDs(queue);
                break;
            case History:
                binding.title.setText("History");
                mainAdapter.setShowIDs(history);
                break;
            default:
                binding.title.setText("Videos");
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
        if (findDataList == null)
            return true;

        for (FindData findData : findDataList)
            if (!findData.matches(videoFile))
                return false;

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

    private void sendMessage(byte[] message) {
        networkService.sendMessage(message);
    }

    private void handleMessage(byte[] bytes) {
        Message message = new Message(bytes);
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
