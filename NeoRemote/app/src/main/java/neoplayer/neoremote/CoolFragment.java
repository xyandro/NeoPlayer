package neoplayer.neoremote;

import android.app.Fragment;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.ListView;

import java.util.ArrayList;

public class CoolFragment extends Fragment {
    private static final String TAG = CoolFragment.class.getSimpleName();

    private final MainActivity mainActivity;
    private final MediaListAdapter adapter;
    private final ArrayList<MediaData> coolVideos;
    private final ArrayList<MediaData> queueVideos;

    public CoolFragment(MainActivity mainActivity, ArrayList<MediaData> coolVideos, ArrayList<MediaData> queueVideos) {
        this.mainActivity = mainActivity;
        this.coolVideos = coolVideos;
        this.queueVideos = queueVideos;
        adapter = new MediaListAdapter(mainActivity, coolVideos, queueVideos);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        View result = inflater.inflate(R.layout.fragment_cool, container, false);

        final EditText searchText = result.findViewById(R.id.search_text);
        searchText.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                adapter.setFilter(charSequence.toString());
            }

            @Override
            public void afterTextChanged(Editable editable) {
            }
        });

        ((ListView) result.findViewById(R.id.videos_list)).setAdapter(adapter);
        result.findViewById(R.id.clear_search).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                searchText.setText("");
            }
        });

        result.findViewById(R.id.back30).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.setPosition(-30, true);
            }
        });

        result.findViewById(R.id.back5).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.setPosition(-5, true);
            }
        });

        result.findViewById(R.id.play).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.play();
            }
        });

        result.findViewById(R.id.forward5).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.setPosition(5, true);
            }
        });

        result.findViewById(R.id.forward30).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.setPosition(30, true);
            }
        });

        result.findViewById(R.id.forward).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.forward();
            }
        });

        return result;
    }

    public void Refresh() {
        adapter.notifyDataSetChanged();
    }
}
