package neoplayer.neoremote;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.ServiceConnection;
import android.os.Bundle;
import android.os.IBinder;
import android.support.v4.content.LocalBroadcastManager;
import android.support.v4.media.VolumeProviderCompat;
import android.support.v4.media.session.MediaSessionCompat;
import android.support.v4.media.session.PlaybackStateCompat;
import android.support.v4.view.PagerAdapter;
import android.support.v4.view.ViewPager;
import android.text.Editable;
import android.text.TextWatcher;
import android.text.format.DateUtils;
import android.util.Log;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.ImageButton;
import android.widget.ListView;
import android.widget.SeekBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.LinkedHashMap;

public class MainActivity extends Activity {
    private static final String TAG = MainActivity.class.getSimpleName();

    private SocketClient socketClient;
    private final ArrayList<MediaData> queueVideos = new ArrayList<>();
    private final ArrayList<MediaData> coolVideos = new ArrayList<>();
    private final ArrayList<MediaData> youTubeVideos = new ArrayList<>();
    private MediaSessionCompat mediaSession;
    private VolumeProviderCompat volumeProvider;
    private boolean userTrackingSeekBar = false;
    private final MediaListAdapter queueAdapter;
    private final MediaListAdapter coolAdapter;
    private static final LinkedHashMap<String, String> validSizes = new LinkedHashMap<>();
    private final MediaListAdapter youTubeAdapter;

    private ViewPager pager;
    private NEEditText queueSearchText;
    private ImageButton queueClearSearch;
    private ListView queueVideosList;
    private NEEditText coolSearchText;
    private ImageButton coolClearSearch;
    private ListView coolVideosList;
    private NEEditText slidesQuery;
    private Spinner slidesSize;
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

    MainActivity() {
        queueAdapter = new MediaListAdapter(this, queueVideos, queueVideos);
        coolAdapter = new MediaListAdapter(this, coolVideos, queueVideos);
        youTubeAdapter = new MediaListAdapter(this, youTubeVideos, queueVideos);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        prepareSocketService();
        prepareMediaSession();
        setupControls();
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
        slidesQuery = findViewById(R.id.slides_query);
        slidesSize = findViewById(R.id.slides_size);
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
                                SocketClient.sendRestart();
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

        ArrayAdapter<String> adapter = new ArrayAdapter<>(this, R.layout.spinner_item, new ArrayList<>(validSizes.keySet()));
        slidesSize.setAdapter(adapter);

        slidesSubmit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                slidesQuery.clearFocus();
                String query = slidesQuery.getText().toString();
                String size = validSizes.get(slidesSize.getSelectedItem());
                socketClient.setSlidesData(query, size);
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
                    socketClient.setSlideDisplayTime(displayTime);
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
                socketClient.cycleSlide(false);
            }
        });

        slidesPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.pauseSlides();
            }
        });

        slidesForward.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.cycleSlide(true);
            }
        });

        youtubeVideosList.setAdapter(youTubeAdapter);
        youtubeSubmit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                youtubeSearchText.clearFocus();
                socketClient.requestYouTube(youtubeSearchText.getText().toString());
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
                socketClient.setPosition(seekBar.getProgress(), false);
                userTrackingSeekBar = false;
            }
        });

        navbarBack30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(-30, true);
            }
        });

        navbarBack5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(-5, true);
            }
        });

        navbarPlay.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.play();
            }
        });

        navbarForward5.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(5, true);
            }
        });

        navbarForward30.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setPosition(30, true);
            }
        });

        navbarForward.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.forward();
            }
        });
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

        volumeProvider = new VolumeProviderCompat(VolumeProviderCompat.VOLUME_CONTROL_ABSOLUTE, 100, 50) {
            @Override
            public void onSetVolumeTo(int volume) {
                socketClient.setVolume(volume, false);
            }

            @Override
            public void onAdjustVolume(int delta) {
                socketClient.setVolume(delta * 5, true);
            }
        };

        mediaSession.setPlaybackToRemote(volumeProvider);
        mediaSession.setActive(true);
    }

    private void prepareSocketService() {
        BroadcastReceiver broadcastReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                handleMessage(intent);
            }
        };
        final LocalBroadcastManager broadcastManager = LocalBroadcastManager.getInstance(this);
        broadcastManager.registerReceiver(broadcastReceiver, new IntentFilter("NeoRemoteEvent"));

        ServiceConnection connection = new ServiceConnection() {
            @Override
            public void onServiceConnected(ComponentName className, IBinder service) {
                socketClient = ((SocketClient.SocketServiceBinder) service).getService();
                socketClient.setBroadcastManager(broadcastManager);
            }

            @Override
            public void onServiceDisconnected(ComponentName arg0) {
                socketClient = null;
            }
        };

        bindService(new Intent(this, SocketClient.class), connection, Context.BIND_AUTO_CREATE);
    }

    private void handleMessage(Intent intent) {
        Bundle extras = intent.getExtras();

        if (extras.containsKey("Queue")) {
            ArrayList<MediaData> mediaDatas = (ArrayList<MediaData>) extras.get("Queue");
            queueVideos.clear();
            for (MediaData mediaData : mediaDatas)
                queueVideos.add(mediaData);
            queueAdapter.notifyDataSetChanged();
            coolAdapter.notifyDataSetChanged();
            youTubeAdapter.notifyDataSetChanged();
        }

        if (extras.containsKey("Cool")) {
            ArrayList<MediaData> mediaDatas = (ArrayList<MediaData>) extras.get("Cool");
            coolVideos.clear();
            for (MediaData mediaData : mediaDatas)
                coolVideos.add(mediaData);
            coolAdapter.notifyDataSetChanged();
        }

        if (extras.containsKey("YouTube")) {
            ArrayList<MediaData> mediaDatas = (ArrayList<MediaData>) extras.get("YouTube");
            youTubeVideos.clear();
            for (MediaData mediaData : mediaDatas)
                youTubeVideos.add(mediaData);
            youTubeAdapter.notifyDataSetChanged();
        }

        if (extras.containsKey("Playing"))
            navbarPlay.setImageResource(extras.getBoolean("Playing") ? R.drawable.pause : R.drawable.play);

        if (extras.containsKey("Title"))
            navbarTitle.setText(extras.getString("Title"));

        if ((extras.containsKey("Position")) && (!userTrackingSeekBar))
            navbarSeekBar.setProgress(extras.getInt("Position"));

        if (extras.containsKey("MaxPosition")) {
            int maxPosition = extras.getInt("MaxPosition");
            navbarSeekBar.setMax(maxPosition);
            navbarMaxTime.setText(DateUtils.formatElapsedTime(maxPosition));
        }

        if (extras.containsKey("Volume"))
            volumeProvider.setCurrentVolume(extras.getInt("Volume"));

        if (extras.containsKey("SlidesQuery"))
            slidesQuery.setText(extras.getString("SlidesQuery"));

        if (extras.containsKey("SlidesSize")) {
            int index = 0;
            for (String entry : validSizes.keySet()) {
                if (validSizes.get(entry).equals(extras.get("SlidesSize")))
                    slidesSize.setSelection(index);
                ++index;
            }
        }

        if (extras.containsKey("SlideDisplayTime"))
            slidesDisplayTime.setProgress(displayTimeToSeekBar(extras.getInt("SlideDisplayTime")));

        if (extras.containsKey("SlidesPaused"))
            slidesPlay.setImageResource(extras.getBoolean("SlidesPaused") ? R.drawable.play : R.drawable.pause);

        if (extras.containsKey("Toast"))
            Toast.makeText(this, extras.getString("Toast"), Toast.LENGTH_SHORT).show();
    }

    public void queueVideo(MediaData mediaData) {
        socketClient.queueVideo(mediaData);
    }

    class ScreenSlidePagerAdapter extends PagerAdapter {
        final int pageCount;

        public ScreenSlidePagerAdapter(ViewPager pager) {
            pageCount = pager.getChildCount();
            pager.setOffscreenPageLimit(pageCount);
            pager.setAdapter(this);
        }

        @Override
        public Object instantiateItem(ViewGroup collection, int position) {
            return collection.getChildAt(position);
        }

        @Override
        public int getCount() {
            return pageCount;
        }

        @Override
        public boolean isViewFromObject(View arg0, Object arg1) {
            return arg0 == arg1;
        }
    }
}
