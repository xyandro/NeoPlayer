package neoplayer.neoremote;

import android.databinding.DataBindingUtil;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;

import java.util.ArrayList;

import neoplayer.neoremote.databinding.DownloadAdapterItemBinding;

public class DownloadAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private ArrayList<DownloadData> list = new ArrayList<>();

    public DownloadAdapter(MainActivity mainActivity) {
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

        DownloadAdapterItemBinding binding = DataBindingUtil.inflate(mainActivity.getLayoutInflater(), R.layout.download_adapter_item, parent, false);
        binding.name.setText(downloadData.title);
        binding.progress.setProgress(downloadData.progress);
        return binding.getRoot();
    }
}
