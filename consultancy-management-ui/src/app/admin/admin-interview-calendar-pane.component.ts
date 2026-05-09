import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatCalendar } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { AdminService, InterviewCalendarEvent } from '../core/services/admin.service';

@Component({
  selector: 'app-admin-interview-calendar-pane',
  standalone: true,
  imports: [DatePipe, MatCalendar, MatNativeDateModule],
  templateUrl: './admin-interview-calendar-pane.component.html',
  styleUrl: './admin-interview-calendar-pane.component.scss'
})
export class AdminInterviewCalendarPaneComponent implements OnInit {
  private readonly admin = inject(AdminService);

  /** Days (local) that have at least one interview. */
  private readonly daysWithEvents = new Set<string>();
  selectedDate: Date = new Date();
  allEvents: InterviewCalendarEvent[] = [];
  eventsForDay: InterviewCalendarEvent[] = [];

  readonly dateClass = (d: Date): string =>
    this.daysWithEvents.has(this.dayKey(d)) ? 'has-interview' : '';

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth() - 1, 1, 0, 0, 0, 0);
    const to = new Date(now.getFullYear(), now.getMonth() + 4, 0, 23, 59, 59, 999);
    this.admin.interviewCalendar(from.toISOString(), to.toISOString()).subscribe({
      next: (rows) => {
        this.allEvents = rows;
        this.rebuildDayIndex();
        this.filterSelectedDay();
      },
      error: () => {
        this.allEvents = [];
        this.daysWithEvents.clear();
        this.eventsForDay = [];
      }
    });
  }

  onSelectedChange(d: Date | null): void {
    if (!d) return;
    this.selectedDate = d;
    this.filterSelectedDay();
  }

  private dayKey(d: Date): string {
    const p = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
  }

  private rebuildDayIndex(): void {
    this.daysWithEvents.clear();
    for (const ev of this.allEvents) {
      const start = new Date(ev.interviewDate);
      const end = ev.interviewEndDate ? new Date(ev.interviewEndDate) : new Date(start);
      const cur = new Date(start.getFullYear(), start.getMonth(), start.getDate());
      const last = new Date(end.getFullYear(), end.getMonth(), end.getDate());
      while (cur <= last) {
        this.daysWithEvents.add(this.dayKey(cur));
        cur.setDate(cur.getDate() + 1);
      }
    }
  }

  private filterSelectedDay(): void {
    const key = this.dayKey(this.selectedDate);
    this.eventsForDay = this.allEvents.filter((ev) => {
      const start = new Date(ev.interviewDate);
      const end = ev.interviewEndDate ? new Date(ev.interviewEndDate) : new Date(start);
      const dayStart = new Date(this.selectedDate.getFullYear(), this.selectedDate.getMonth(), this.selectedDate.getDate());
      const dayEnd = new Date(dayStart);
      dayEnd.setHours(23, 59, 59, 999);
      return start <= dayEnd && end >= dayStart;
    });
  }

  timeRange(ev: InterviewCalendarEvent): string {
    const start = new Date(ev.interviewDate);
    const fmt = (d: Date) =>
      d.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
    if (!ev.interviewEndDate) return fmt(start);
    const end = new Date(ev.interviewEndDate);
    return `${fmt(start)} – ${fmt(end)}`;
  }
}
