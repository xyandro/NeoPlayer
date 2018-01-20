package neoplayer.neoremote;

import android.app.Dialog;
import android.app.DialogFragment;
import android.databinding.DataBindingUtil;
import android.os.Bundle;
import android.support.annotation.Nullable;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;

import neoplayer.neoremote.databinding.SortDialogBinding;

public class SortDialog extends DialogFragment {
    private MainActivity mainActivity;
    private SortDialogBinding binding;
    private SortData sortData;
    private SortAdapter sortAdapter;

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        Dialog dialog = super.onCreateDialog(savedInstanceState);
        dialog.getWindow().requestFeature(Window.FEATURE_NO_TITLE);
        return dialog;
    }

    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, Bundle savedInstanceState) {
        binding = DataBindingUtil.inflate(inflater, R.layout.sort_dialog, container, false);

        binding.clear.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                sortData.clear();
                sortAdapter.notifyDataSetChanged();
            }
        });
        binding.submit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.setSortData(sortData);
                dismiss();
            }
        });
        sortAdapter = new SortAdapter(mainActivity, sortData);
        binding.list.setAdapter(sortAdapter);

        return binding.getRoot();
    }

    public static SortDialog createDialog(MainActivity mainActivity, SortData sortData) {
        SortDialog sortDialog = new SortDialog();
        sortDialog.mainActivity = mainActivity;
        sortDialog.sortData = sortData.copy();
        return sortDialog;
    }
}