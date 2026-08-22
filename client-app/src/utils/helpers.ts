import { toast } from 'react-toastify';

export const showErrorToast = (toastMessage: string, error: unknown) => {
  toast.error(`${toastMessage} failed: ${getErrorMessage(error)}`, {
    position: 'top-right',
    autoClose: 2000,
  });
};

export const getErrorMessage = (error: unknown) => {
  const fullMessage = error instanceof Error ? error.message : String(error);

  const hubIndex = fullMessage.indexOf('HubException: ');

  if (hubIndex !== -1) {
    return fullMessage.substring(hubIndex + 'HubException: '.length);
  }

  return fullMessage;
};
