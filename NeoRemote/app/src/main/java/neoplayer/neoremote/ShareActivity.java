package neoplayer.neoremote;

import android.app.Activity;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.os.Bundle;
import android.os.IBinder;

public class ShareActivity extends Activity {
    private static final String TAG = ShareActivity.class.getSimpleName();

    private ServiceConnection serviceConnection;
    private NetworkService networkService;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        serviceConnection = new ServiceConnection() {
            @Override
            public void onServiceConnected(ComponentName className, IBinder service) {
                networkService = ((NetworkService.NetworkServiceBinder) service).getService();
                String url = getIntent().getStringExtra(Intent.EXTRA_TEXT);
                networkService.sendMessage(new Message().add("DownloadURL").add(url).toArray());
                finish();
            }

            @Override
            public void onServiceDisconnected(ComponentName arg0) {
                finish();
            }
        };

        bindService(new Intent(this, NetworkService.class), serviceConnection, Context.BIND_AUTO_CREATE);
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        unbindService(serviceConnection);
    }
}
