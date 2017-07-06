package neoplayer.neoremote;

import android.app.AlertDialog;
import android.app.Dialog;
import android.app.DialogFragment;
import android.content.DialogInterface;
import android.os.Bundle;
import android.view.View;
import android.widget.EditText;

public class GetAddressDialog extends DialogFragment {
    private final SocketClient socketClient;
    private final String address;
    EditText addressText;

    public GetAddressDialog(SocketClient socketClient, String address) {
        this.socketClient = socketClient;
        this.address = address;
    }

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        AlertDialog dialog = new AlertDialog.Builder(getActivity())
                .setMessage("Find NeoPlayer")
                .setPositiveButton("Ok", null)
                .setNegativeButton("Detect", null)
                .setView(R.layout.get_address_dialog)
                .create();
        return dialog;
    }

    @Override
    public void onResume() {
        super.onResume();
        AlertDialog dialog = (AlertDialog) getDialog();
        dialog.setCancelable(false);
        addressText = dialog.findViewById(R.id.address);
        addressText.setText(address);
        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                socketClient.setAddress(addressText.getText().toString());
            }
        });
        dialog.getButton(AlertDialog.BUTTON_NEGATIVE).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                String newAddress = socketClient.findNeoPlayer();
                if (newAddress != null)
                    addressText.setText(newAddress);
            }
        });
    }
}
