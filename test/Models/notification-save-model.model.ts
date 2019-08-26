import { NotificationAction } from "./notification-action.model"

export class NotificationSaveModel {
  title: string;
  dir: string;
  lang: string;
  body: string;
  tag: string;
  image: string;
  icon: string;
  badge: string;
  vibrate: Array<number>;
  timestamp: number;
  renotify: boolean;
  silent: boolean;
  requireInteraction: boolean;
  data: object;
  actions: Array<NotificationAction>;
}