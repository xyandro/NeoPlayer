package neoplayer.neoremote;

import android.text.Editable;
import android.text.TextWatcher;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.EditText;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.HashMap;
import java.util.Map;

public class FindAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private final HashMap<String, String> tags;
    private ArrayList<Map.Entry<String, String>> list;

    public FindAdapter(MainActivity mainActivity, HashMap<String, String> tags) {
        super();
        this.mainActivity = mainActivity;
        this.tags = tags;
        updateKeys();
    }

    private void updateKeys() {
        list = new ArrayList<>(tags.entrySet());
        Collections.sort(list, new Comparator<Map.Entry<String, String>>() {
            @Override
            public int compare(Map.Entry<String, String> entry1, Map.Entry<String, String> entry2) {
                return entry1.getKey().compareTo(entry2.getKey());
            }
        });
    }

    @Override
    public int getCount() {
        return list.size();
    }

    @Override
    public Object getItem(int i) {
        return list.get(i);
    }

    @Override
    public long getItemId(int i) {
        return i;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        final Map.Entry<String, String> value = (Map.Entry<String, String>) getItem(position);

        View view = mainActivity.getLayoutInflater().inflate(R.layout.fragment_edittags_listitem, parent, false);

        ((TextView) view.findViewById(R.id.name)).setText(value.getKey());

        ((EditText) view.findViewById(R.id.value)).setText(value.getValue());
        ((EditText) view.findViewById(R.id.value)).addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void afterTextChanged(Editable editable) {
                value.setValue(editable.toString().trim().toLowerCase());
            }
        });

        return view;
    }
}
