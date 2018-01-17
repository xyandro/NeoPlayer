package neoplayer.neoremote;

import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ProgressBar;
import android.widget.TextView;

import java.util.ArrayList;

public class DownloadListAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private ArrayList<DownloadData> list = new ArrayList<>();

    public DownloadListAdapter(MainActivity mainActivity) {
        super();
        this.mainActivity = mainActivity;
    }

    public void setDownloadData(ArrayList<DownloadData> list) {
        this.list = list;
        notifyDataSetChanged();
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
        final DownloadData downloadData = list.get(position);

        View view = convertView;
        if (view == null)
            view = mainActivity.getLayoutInflater().inflate(R.layout.fragment_download_listitem, parent, false);
        ((TextView) view.findViewById(R.id.name)).setText(downloadData.title);
        ((ProgressBar) view.findViewById(R.id.progress)).setProgress(downloadData.progress);
        return view;
    }
}
